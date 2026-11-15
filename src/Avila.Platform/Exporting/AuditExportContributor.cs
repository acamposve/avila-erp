using System.Text.Json;
using Avila.Platform.Exporting;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Exporting;

public sealed class AuditExportContributor : ITenantExportContributor
{
    public string ModuleKey => "audit";
    public int Order => 30;
    public int SchemaVersion => 1;

    public async Task<TenantExportSection> ExportAsync(
        IUnitOfWork unitOfWork,
        TenantExportContext context,
        CancellationToken cancellationToken = default)
    {
        await using var db = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(db);

        var events = await db.AuditEvents
            .AsNoTracking()
            .Where(x => x.TenantId == context.TenantId)
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var records = events.Select(audit => new TenantExportRecord(
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
            }))).ToList();

        return new TenantExportSection(ModuleKey, SchemaVersion, records);
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
