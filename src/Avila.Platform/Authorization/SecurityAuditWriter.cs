using Avila.Platform.Identity;
using Avila.Platform.Tenancy;

namespace Avila.Platform.Authorization;

/// <summary>
/// Writes denied attempts in an independent connection/transaction. The
/// offending business transaction may roll back; this audit record must not.
/// </summary>
public sealed class SecurityAuditWriter
{
    private readonly TenantUnitOfWorkFactory _factory;
    private readonly string _connectionString;

    public SecurityAuditWriter(TenantUnitOfWorkFactory factory, string connectionString)
    {
        _factory = factory;
        _connectionString = connectionString;
    }

    public async Task RecordDeniedAsync(
        Guid targetTenantId,
        Guid actorUserId,
        Guid actorTenantId,
        string permission,
        string reason,
        AccessScopeRef? scope,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        if (targetTenantId == Guid.Empty)
        {
            throw new ArgumentException("Target tenant is required.", nameof(targetTenantId));
        }

        // A fresh UoW means a separate connection and transaction. It commits
        // before returning, independently of the caller's rollback.
        await using var unitOfWork = await _factory.BeginAsync(
            _connectionString,
            targetTenantId,
            cancellationToken);

        var auditActor = new SessionPrincipal(
            actorUserId,
            targetTenantId,
            "security-audit@internal",
            "system");

        await new AuditService().RecordAsync(
            unitOfWork,
            auditActor,
            "authorization.denied",
            "permission",
            entityId: null,
            before: null,
            after: new
            {
                permission,
                reason,
                actorTenantId,
                targetTenantId,
                scopeType = scope?.Type,
                scopeId = scope?.Id
            },
            correlationId ?? Guid.NewGuid().ToString("N"),
            cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);
    }
}
