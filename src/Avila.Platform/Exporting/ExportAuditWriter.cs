using Avila.Platform.Authorization;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;

namespace Avila.Platform.Exporting;

/// <summary>
/// Writes export lifecycle events in independent transactions so a failed or
/// rolled-back export still leaves the requested/failed trail.
/// </summary>
public sealed class ExportAuditWriter
{
    private readonly TenantUnitOfWorkFactory _factory;
    private readonly string _connectionString;

    public ExportAuditWriter(TenantUnitOfWorkFactory factory, string connectionString)
    {
        _factory = factory;
        _connectionString = connectionString;
    }

    public async Task RecordAsync(
        Guid tenantId,
        Guid actorUserId,
        string action,
        string correlationId,
        object? metadata,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = await _factory.BeginAsync(
            _connectionString,
            tenantId,
            cancellationToken);

        var actor = new SessionPrincipal(
            actorUserId,
            tenantId,
            "export-audit@internal",
            "system");

        await new AuditService().RecordAsync(
            unitOfWork,
            actor,
            action,
            "tenant_export",
            entityId: null,
            before: null,
            after: metadata,
            correlationId,
            cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);
    }
}
