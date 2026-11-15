using System.Text.Json;
using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Avila.Platform.Authorization;

/// <summary>
/// Append-only audit writer. The database trigger and grants prevent updates
/// and deletes; this service only inserts.
/// </summary>
public sealed class AuditService
{
    public async Task<Guid> RecordAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        string action,
        string entityType,
        Guid? entityId,
        object? before,
        object? after,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (actor.TenantId != unitOfWork.TenantId)
        {
            throw new AuthorizationException("audit.tenant.mismatch");
        }

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        await using (var lockCommand = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(@tenant, 0))",
            unitOfWork.Connection,
            unitOfWork.Transaction))
        {
            lockCommand.Parameters.AddWithValue("tenant", unitOfWork.TenantId.ToString("D"));
            await lockCommand.ExecuteScalarAsync(cancellationToken);
        }

        var previousHash = await context.AuditEvents
            .AsNoTracking()
            .Where(x => x.TenantId == unitOfWork.TenantId && x.HashVersion == 1)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Select(x => x.EventHash)
            .FirstOrDefaultAsync(cancellationToken);

        var audit = new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            ActorUserId = actor.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after),
            CorrelationId = string.IsNullOrWhiteSpace(correlationId)
                ? Guid.NewGuid().ToString("N")
                : correlationId,
            OccurredAt = TruncateToMicroseconds(DateTimeOffset.UtcNow),
            PreviousHash = previousHash,
            HashVersion = 1
        };
        audit.EventHash = AuditHash.Compute(audit);

        context.AuditEvents.Add(audit);
        await context.SaveChangesAsync(cancellationToken);
        return audit.Id;
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
        => new(value.Ticks - (value.Ticks % 10), value.Offset);
}
