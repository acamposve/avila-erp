using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avila.Platform.Tenancy;

namespace Avila.Platform.Exporting;

public static class TenantExportFormat
{
    public const string Version = "avila.tenant-export/v1";
}

public sealed record TenantExportContext(Guid TenantId, string FormatVersion = TenantExportFormat.Version);

public sealed record TenantExportRecord(string Entity, string Id, JsonElement Data);

public sealed record TenantExportSection(
    string ModuleKey,
    int SchemaVersion,
    IReadOnlyList<TenantExportRecord> Records);

public interface ITenantExportContributor
{
    string ModuleKey { get; }
    int Order { get; }
    int SchemaVersion { get; }

    Task<TenantExportSection> ExportAsync(
        IUnitOfWork unitOfWork,
        TenantExportContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional streaming extension. R1 contributors implement it so the endpoint
/// never materializes a whole tenant export.
/// </summary>
public interface ITenantExportStreamContributor
{
    IAsyncEnumerable<TenantExportRecord> StreamAsync(
        IUnitOfWork unitOfWork,
        TenantExportContext context,
        CancellationToken cancellationToken = default);
}

public sealed record TenantExportSectionManifest(
    string ModuleKey,
    int Order,
    int SchemaVersion,
    int RecordCount,
    string Sha256);

public sealed record TenantExportManifest(
    string FormatVersion,
    Guid TenantId,
    IReadOnlyList<TenantExportSectionManifest> Sections,
    string Sha256);

public sealed record TenantExportEnvelope(
    DateTimeOffset GeneratedAt,
    TenantExportManifest Manifest,
    IReadOnlyList<TenantExportSection> Sections);

/// <summary>
/// Composes module-owned export sections. It has no reference to Administration,
/// Sales, Inventory or Billing; modules register an ITenantExportContributor at
/// the composition root.
/// </summary>
public sealed class TenantExportRegistry
{
    private readonly IReadOnlyList<ITenantExportContributor> _contributors;

    public TenantExportRegistry(IEnumerable<ITenantExportContributor> contributors)
    {
        var ordered = contributors
            .OrderBy(x => x.Order)
            .ThenBy(x => x.ModuleKey, StringComparer.Ordinal)
            .ToList();

        if (ordered.Count == 0)
        {
            throw new ArgumentException("At least one export contributor is required.", nameof(contributors));
        }

        if (ordered.Any(x => string.IsNullOrWhiteSpace(x.ModuleKey)))
        {
            throw new ArgumentException("Every export contributor needs a module key.", nameof(contributors));
        }

        if (ordered.GroupBy(x => x.ModuleKey, StringComparer.Ordinal).Any(x => x.Count() > 1))
        {
            throw new ArgumentException("Duplicate export module keys are not allowed.", nameof(contributors));
        }

        if (ordered.Any(x => x.SchemaVersion < 1 || x.Order < 0))
        {
            throw new ArgumentException("Export schema version must be >= 1 and order >= 0.", nameof(contributors));
        }

        _contributors = ordered;
    }

    public async Task<TenantExportEnvelope> ExportAsync(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        var sections = await ComposeAsync(unitOfWork, cancellationToken);
        var canonical = CanonicalJson.Build(unitOfWork.TenantId, sections);
        var contentHash = CanonicalJson.Hash(canonical);
        var manifestSections = sections
            .Select(section => new TenantExportSectionManifest(
                section.ModuleKey,
                _contributors.Single(x => x.ModuleKey == section.ModuleKey).Order,
                section.SchemaVersion,
                section.Records.Count,
                CanonicalJson.Hash(CanonicalJson.BuildSection(section))))
            .ToList();

        var manifest = new TenantExportManifest(
            TenantExportFormat.Version,
            unitOfWork.TenantId,
            manifestSections,
            contentHash);

        return new TenantExportEnvelope(DateTimeOffset.UtcNow, manifest, sections);
    }

    /// <summary>
    /// Streams deterministic NDJSON and returns the SHA-256 manifest. Only one
    /// record and its canonical bytes are held at a time.
    /// </summary>
    public async Task<TenantExportManifest> StreamNdjsonAsync(
        IUnitOfWork unitOfWork,
        Stream output,
        CancellationToken cancellationToken = default)
    {
        var context = new TenantExportContext(unitOfWork.TenantId);
        var sectionManifests = new List<TenantExportSectionManifest>(_contributors.Count);
        var generatedAt = DateTimeOffset.UtcNow;

        await WriteLineAsync(output, writer =>
        {
            writer.WriteString("type", "export");
            writer.WriteString("format", TenantExportFormat.Version);
            writer.WriteString("tenant_id", unitOfWork.TenantId);
            writer.WriteString("generated_at", generatedAt);
        }, cancellationToken);

        foreach (var contributor in _contributors)
        {
            await WriteLineAsync(output, writer =>
            {
                writer.WriteString("type", "section");
                writer.WriteString("module", contributor.ModuleKey);
                writer.WriteNumber("order", contributor.Order);
                writer.WriteNumber("schema_version", contributor.SchemaVersion);
            }, cancellationToken);

            using var sectionHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            sectionHash.AppendData(CanonicalJson.BuildSectionStart(
                contributor.ModuleKey,
                contributor.SchemaVersion));
            var count = 0;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var first = true;

            await foreach (var record in StreamRecordsAsync(
                               contributor,
                               unitOfWork,
                               context,
                               cancellationToken))
            {
                ValidateStreamRecord(contributor.ModuleKey, record);
                var key = $"{record.Entity}\u001f{record.Id}";
                if (!seen.Add(key))
                {
                    throw new InvalidOperationException(
                        $"Section {contributor.ModuleKey} has duplicate {record.Entity}/{record.Id}.");
                }

                if (!first)
                {
                    sectionHash.AppendData(","u8);
                }

                first = false;
                sectionHash.AppendData(CanonicalJson.BuildRecord(record));
                count++;

                await WriteLineAsync(output, writer =>
                {
                    writer.WriteString("type", "record");
                    writer.WriteString("module", contributor.ModuleKey);
                    writer.WriteString("entity", record.Entity);
                    writer.WriteString("id", record.Id);
                    writer.WritePropertyName("data");
                    record.Data.WriteTo(writer);
                }, cancellationToken);
            }

            sectionHash.AppendData("]}"u8);
            var sectionSha = Convert.ToHexString(
                sectionHash.GetHashAndReset()).ToLowerInvariant();
            var sectionManifest = new TenantExportSectionManifest(
                contributor.ModuleKey,
                contributor.Order,
                contributor.SchemaVersion,
                count,
                sectionSha);
            sectionManifests.Add(sectionManifest);

            await WriteLineAsync(output, writer =>
            {
                writer.WriteString("type", "section_end");
                writer.WriteString("module", contributor.ModuleKey);
                writer.WriteNumber("record_count", count);
                writer.WriteString("sha256", sectionSha);
            }, cancellationToken);
        }

        var manifest = new TenantExportManifest(
            TenantExportFormat.Version,
            unitOfWork.TenantId,
            sectionManifests,
            CanonicalJson.Hash(CanonicalJson.BuildManifest(unitOfWork.TenantId, sectionManifests)));

        await WriteLineAsync(output, writer =>
        {
            writer.WriteString("type", "manifest");
            writer.WriteString("format", manifest.FormatVersion);
            writer.WriteString("tenant_id", manifest.TenantId);
            writer.WriteStartArray("sections");
            foreach (var section in manifest.Sections)
            {
                writer.WriteStartObject();
                writer.WriteString("module", section.ModuleKey);
                writer.WriteNumber("order", section.Order);
                writer.WriteNumber("schema_version", section.SchemaVersion);
                writer.WriteNumber("record_count", section.RecordCount);
                writer.WriteString("sha256", section.Sha256);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteString("sha256", manifest.Sha256);
        }, cancellationToken);

        return manifest;
    }

    private async Task<List<TenantExportSection>> ComposeAsync(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var context = new TenantExportContext(unitOfWork.TenantId);
        var sections = new List<TenantExportSection>(_contributors.Count);

        foreach (var contributor in _contributors)
        {
            var section = await contributor.ExportAsync(unitOfWork, context, cancellationToken);
            if (!string.Equals(section.ModuleKey, contributor.ModuleKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Contributor {contributor.ModuleKey} returned section {section.ModuleKey}.");
            }

            ValidateRecords(section);
            sections.Add(section with
            {
                Records = section.Records
                    .OrderBy(x => x.Entity, StringComparer.Ordinal)
                    .ThenBy(x => x.Id, StringComparer.Ordinal)
                    .ToList()
            });
        }

        return sections;
    }

    private static async IAsyncEnumerable<TenantExportRecord> StreamRecordsAsync(
        ITenantExportContributor contributor,
        IUnitOfWork unitOfWork,
        TenantExportContext context,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (contributor is ITenantExportStreamContributor streaming)
        {
            await foreach (var record in streaming.StreamAsync(unitOfWork, context, cancellationToken)
                               .WithCancellation(cancellationToken))
            {
                yield return record;
            }

            yield break;
        }

        var section = await contributor.ExportAsync(unitOfWork, context, cancellationToken);
        ValidateRecords(section);
        foreach (var record in section.Records
                     .OrderBy(x => x.Entity, StringComparer.Ordinal)
                     .ThenBy(x => x.Id, StringComparer.Ordinal))
        {
            yield return record;
        }
    }

    private static async Task WriteLineAsync(
        Stream output,
        Action<Utf8JsonWriter> write,
        CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            write(writer);
            writer.WriteEndObject();
            await writer.FlushAsync(cancellationToken);
        }

        buffer.WriteByte((byte)'\n');
        await output.WriteAsync(buffer.ToArray(), cancellationToken);
    }

    private static void ValidateStreamRecord(string module, TenantExportRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.Entity) || string.IsNullOrWhiteSpace(record.Id))
        {
            throw new InvalidOperationException($"Section {module} has a record without entity/id.");
        }

        if (record.Data.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Section {module} record {record.Entity}/{record.Id} data must be a JSON object.");
        }
    }

    private static void ValidateRecords(TenantExportSection section)
    {
        if (section.Records is null)
        {
            throw new InvalidOperationException($"Section {section.ModuleKey} has no records collection.");
        }

        foreach (var record in section.Records)
        {
            if (string.IsNullOrWhiteSpace(record.Entity) || string.IsNullOrWhiteSpace(record.Id))
            {
                throw new InvalidOperationException($"Section {section.ModuleKey} has a record without entity/id.");
            }

            if (record.Data.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"Section {section.ModuleKey} record {record.Entity}/{record.Id} data must be a JSON object.");
            }
        }

        var duplicates = section.Records
            .GroupBy(x => (x.Entity, x.Id))
            .Where(x => x.Count() > 1)
            .Select(x => $"{x.Key.Entity}/{x.Key.Id}")
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                $"Section {section.ModuleKey} has duplicate records: {string.Join(", ", duplicates)}.");
        }
    }
}

internal static class CanonicalJson
{
    public static byte[] Build(Guid tenantId, IReadOnlyList<TenantExportSection> sections)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("format", TenantExportFormat.Version);
            writer.WriteString("tenant_id", tenantId.ToString("D"));
            writer.WriteStartArray("sections");
            foreach (var section in sections)
            {
                WriteSection(writer, section);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static byte[] BuildSection(TenantExportSection section)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteSection(writer, section);
        }

        return stream.ToArray();
    }

    public static byte[] BuildSectionStart(string moduleKey, int schemaVersion)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("module", moduleKey);
            writer.WriteNumber("schema_version", schemaVersion);
            writer.WriteStartArray("records");
        }

        return stream.ToArray();
    }

    public static byte[] BuildRecord(TenantExportRecord record)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteRecord(writer, record);
        }

        return stream.ToArray();
    }

    public static byte[] BuildManifest(
        Guid tenantId,
        IReadOnlyList<TenantExportSectionManifest> sections)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("format", TenantExportFormat.Version);
            writer.WriteString("tenant_id", tenantId.ToString("D"));
            writer.WriteStartArray("sections");
            foreach (var section in sections)
            {
                writer.WriteStartObject();
                writer.WriteString("module", section.ModuleKey);
                writer.WriteNumber("order", section.Order);
                writer.WriteNumber("schema_version", section.SchemaVersion);
                writer.WriteNumber("record_count", section.RecordCount);
                writer.WriteString("sha256", section.Sha256);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static string Hash(byte[] canonical)
        => Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();

    private static void WriteSection(Utf8JsonWriter writer, TenantExportSection section)
    {
        writer.WriteStartObject();
        writer.WriteString("module", section.ModuleKey);
        writer.WriteNumber("schema_version", section.SchemaVersion);
        writer.WriteStartArray("records");
        foreach (var record in section.Records)
        {
            WriteRecord(writer, record);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteRecord(Utf8JsonWriter writer, TenantExportRecord record)
    {
        writer.WriteStartObject();
        writer.WriteString("entity", record.Entity);
        writer.WriteString("id", record.Id);
        writer.WritePropertyName("data");
        WriteCanonical(writer, record.Data);
        writer.WriteEndObject();
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject()
                             .OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonical(writer, item);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText(), skipInputValidation: false);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }
}
