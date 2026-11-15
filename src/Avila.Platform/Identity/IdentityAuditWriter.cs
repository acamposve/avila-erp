using Avila.Platform.Authorization;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;

namespace Avila.Platform.Identity;

/// <summary>
/// Tenant-scoped security audit for authentication failures. It uses an
/// independent transaction and never records passwords or tokens.
/// </summary>
public sealed class IdentityAuditWriter
{
    private readonly TenantUnitOfWorkFactory _factory;
    private readonly string _connectionString;

    public IdentityAuditWriter(TenantUnitOfWorkFactory factory, string connectionString)
    {
        _factory = factory;
        _connectionString = connectionString;
    }

    public async Task RecordLoginFailureAsync(
        Guid tenantId,
        Guid userId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(_connectionString))
        {
            return;
        }

        await using var unitOfWork = await _factory.BeginAsync(
            _connectionString,
            tenantId,
            cancellationToken);
        var actor = new SessionPrincipal(userId, tenantId, "auth-event@internal", "system");
        await new AuditService().RecordAsync(
            unitOfWork,
            actor,
            "auth.login.failed",
            "identity_user",
            userId,
            before: null,
            after: new { reason = "invalid_credentials" },
            correlationId,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
