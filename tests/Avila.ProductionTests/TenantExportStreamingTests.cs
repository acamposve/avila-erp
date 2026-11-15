using System.Text;
using System.Text.Json;
using Avila.Platform.Exporting;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.ProductionTests;

public sealed class TenantExportStreamingTests
{
    [Fact]
    public async Task Ndjson_stream_is_deterministic_and_manifest_hash_is_stable()
    {
        var tenant = Guid.NewGuid();
        var records = new List<TenantExportRecord>
        {
            Record("company", "2", new { z = 1, a = 2 }),
            Record("company", "1", new { z = 1, a = 2 })
        };
        var registry = new TenantExportRegistry([new FakeContributor(records)]);

        var first = await StreamAsync(registry, tenant);
        var second = await StreamAsync(registry, tenant);

        Assert.Equal(first.Manifest.Sha256, second.Manifest.Sha256);
        Assert.Equal(first.Manifest.Sections[0].Sha256, second.Manifest.Sections[0].Sha256);
        Assert.Equal(64, first.Manifest.Sha256.Length);
        var text = Encoding.UTF8.GetString(first.Body);
        Assert.Contains("\"type\":\"record\"", text);
        Assert.Contains("\"type\":\"manifest\"", text);
        Assert.Contains("\"module\":\"test\"", text);
    }

    [Fact]
    public async Task Streaming_uses_async_records_without_materializing_a_section()
    {
        var contributor = new FakeContributor(
        [
            Record("company", "1", new { name = "one" }),
            Record("company", "2", new { name = "two" })
        ]);
        var registry = new TenantExportRegistry([contributor]);

        var result = await StreamAsync(registry, Guid.NewGuid());

        Assert.Equal(2, result.Manifest.Sections[0].RecordCount);
    }

    private static async Task<(byte[] Body, TenantExportManifest Manifest)> StreamAsync(
        TenantExportRegistry registry,
        Guid tenant)
    {
        await using var output = new MemoryStream();
        var manifest = await registry.StreamNdjsonAsync(
            new FakeUnitOfWork(tenant),
            output);
        return (output.ToArray(), manifest);
    }

    private static TenantExportRecord Record(string entity, string id, object data)
        => new(entity, id, JsonSerializer.SerializeToElement(data));

    private sealed class FakeContributor : ITenantExportContributor, ITenantExportStreamContributor
    {
        private readonly IReadOnlyList<TenantExportRecord> _records;

        public FakeContributor(IReadOnlyList<TenantExportRecord> records)
        {
            _records = records;
        }

        public string ModuleKey => "test";
        public int Order => 10;
        public int SchemaVersion => 1;

        public Task<TenantExportSection> ExportAsync(
            IUnitOfWork unitOfWork,
            TenantExportContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new TenantExportSection(ModuleKey, SchemaVersion, _records));

        public async IAsyncEnumerable<TenantExportRecord> StreamAsync(
            IUnitOfWork unitOfWork,
            TenantExportContext context,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var record in _records)
            {
                await Task.Yield();
                yield return record;
            }
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(Guid tenantId)
        {
            TenantId = tenantId;
        }

        public Guid TenantId { get; }
        public Npgsql.NpgsqlConnection Connection => throw new NotSupportedException();
        public Npgsql.NpgsqlTransaction Transaction => throw new NotSupportedException();

        public DbContextOptions<T> OptionsFor<T>(string migrationSchema) where T : DbContext
            => throw new NotSupportedException();

        public void Enlist(DbContext context) => throw new NotSupportedException();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
