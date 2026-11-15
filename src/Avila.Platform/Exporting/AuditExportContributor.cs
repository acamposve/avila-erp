using System.Runtime.CompilerServices;
using System.Text.Json;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Exporting;

public sealed class AuditExportContributor : ITenantExportContributor, ITenantExportStreamContributor
{
    public string ModuleKey => "audit";
    public int Order => 30;
    public int SchemaVersion => 1;

    public async Task<TenantExportSection> ExportAsync(
        IUnitOfWork unitOfWork,
        TenantExportContext context,
        CancellationToken cancellationToken = default)
    {
        var records = new List<TenantExportRecord>();
        await foreach (var record in StreamAsync(unitOfWork, context, cancellationToken))
        {
            records.Add(record);
        }

        return new TenantExportSection(ModuleKey, SchemaVersion, records);
    }

    public async IAsyncEnumerable<TenantExportRecord> StreamAsync(
        IUnitOfWork unitOfWork,
        TenantExportContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var db = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(db);

        var events = db.AuditEvents
            .AsNoTracking()
            .Where(x => x.TenantId == context.TenantId)
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.Id)
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken);

        await foreach (var audit in events)
        {
            yield return new TenantExportRecord(
                "audit_event",
                audit.Id.ToString("D"),
                JsonSerializer.SerializeToElement(new
                {
                    action = audit.Action,
                    actor_user_id = audit.ActorUserId,
                    entity_type = audit.EntityType,
                    entity_id = audit.EntityId,
                    before = ParseJson(audit.BeforeJson),
                    after = ParseJson(audit.AfterJson),
                    correlation_id = audit.CorrelationId,
                    occurred_at = audit.OccurredAt
                }));
        }
    }

    private static JsonElement? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
