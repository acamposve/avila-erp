using System.Text.Json;
using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;

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
                : correlationId
        };

        context.AuditEvents.Add(audit);
        await context.SaveChangesAsync(cancellationToken);
        return audit.Id;
    }
}
