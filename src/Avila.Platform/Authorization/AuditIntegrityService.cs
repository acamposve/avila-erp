using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Authorization;

public sealed record AuditVerificationResult(
    bool IsValid,
    int CheckedEvents,
    Guid? FirstInvalidEventId,
    string? Reason);

public sealed class AuditIntegrityService
{
    public async Task<AuditVerificationResult> VerifyAsync(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var events = await context.AuditEvents
            .AsNoTracking()
            .Where(x => x.TenantId == unitOfWork.TenantId)
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        string? previous = null;
        var checkedEvents = 0;
        foreach (var audit in events.Where(x => x.HashVersion == 1))
        {
            checkedEvents++;
            if (string.IsNullOrWhiteSpace(audit.EventHash)
                || !string.Equals(audit.PreviousHash, previous, StringComparison.Ordinal))
            {
                return new AuditVerificationResult(
                    false,
                    checkedEvents,
                    audit.Id,
                    "previous_hash does not match the chain.");
            }

            if (!string.Equals(audit.EventHash, AuditHash.Compute(audit), StringComparison.Ordinal))
            {
                return new AuditVerificationResult(
                    false,
                    checkedEvents,
                    audit.Id,
                    "event_hash does not match the canonical event.");
            }

            previous = audit.EventHash;
        }

        return new AuditVerificationResult(true, checkedEvents, null, null);
    }
}

public sealed record AuditEventView(
    Guid Id,
    Guid TenantId,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? BeforeJson,
    string? AfterJson,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    string? PreviousHash,
    string? EventHash);

public sealed record AuditQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Action = null,
    Guid? ActorUserId = null,
    string? EntityType = null);

public sealed record AuditPage(
    IReadOnlyList<AuditEventView> Items,
    string? NextCursor);

public sealed class AuditQueryService
{
    public async Task<AuditPage> QueryAsync(
        IUnitOfWork unitOfWork,
        AuditQuery query,
        string? cursor = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 200);
        var offset = DecodeCursor(cursor);
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var filtered = Filter(context.AuditEvents.AsNoTracking(), unitOfWork.TenantId, query);
        var rows = await filtered
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.Id)
            .Skip(offset)
            .Take(limit + 1)
            .Select(x => new AuditEventView(
                x.Id,
                x.TenantId,
                x.ActorUserId,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.BeforeJson,
                x.AfterJson,
                x.CorrelationId,
                x.OccurredAt,
                x.PreviousHash,
                x.EventHash))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new AuditPage(rows, hasMore ? EncodeCursor(offset + limit) : null);
    }

    public async Task<int> ExportNdjsonAsync(
        IUnitOfWork unitOfWork,
        AuditQuery query,
        Stream output,
        CancellationToken cancellationToken = default)
    {
        var cursor = (string?)null;
        var count = 0;
        do
        {
            var page = await QueryAsync(unitOfWork, query, cursor, 200, cancellationToken);
            foreach (var audit in page.Items)
            {
                var line = System.Text.Json.JsonSerializer.Serialize(new
                {
                    type = "audit",
                    id = audit.Id,
                    tenant_id = audit.TenantId,
                    actor_user_id = audit.ActorUserId,
                    action = audit.Action,
                    entity_type = audit.EntityType,
                    entity_id = audit.EntityId,
                    before = audit.BeforeJson,
                    after = audit.AfterJson,
                    correlation_id = audit.CorrelationId,
                    occurred_at = audit.OccurredAt,
                    previous_hash = audit.PreviousHash,
                    event_hash = audit.EventHash
                });
                await output.WriteAsync(
                    System.Text.Encoding.UTF8.GetBytes(line + "\n"),
                    cancellationToken);
                count++;
            }

            cursor = page.NextCursor;
        }
        while (cursor is not null);

        var manifest = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "manifest",
            record_count = count
        });
        await output.WriteAsync(
            System.Text.Encoding.UTF8.GetBytes(manifest + "\n"),
            cancellationToken);
        return count;
    }

    private static IQueryable<AuditEvent> Filter(
        IQueryable<AuditEvent> source,
        Guid tenantId,
        AuditQuery query)
    {
        source = source.Where(x => x.TenantId == tenantId);
        if (query.From.HasValue)
        {
            source = source.Where(x => x.OccurredAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            source = source.Where(x => x.OccurredAt <= query.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            source = source.Where(x => x.Action == query.Action);
        }

        if (query.ActorUserId.HasValue)
        {
            source = source.Where(x => x.ActorUserId == query.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            source = source.Where(x => x.EntityType == query.EntityType);
        }

        return source;
    }

    private static string EncodeCursor(int offset)
        => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(offset.ToString()));

    private static int DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return 0;
        }

        try
        {
            return Math.Max(
                0,
                int.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor))));
        }
        catch
        {
            throw new InvalidOperationException("AUDIT_CURSOR_INVALID");
        }
    }
}
