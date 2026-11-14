using Avila.Modules.Administration.Contracts;
using Avila.Platform.Tenancy;

namespace Avila.Application;

/// <summary>
/// Tenant-scoped export. The UoW tenant is the only tenant reachable through
/// RLS; there is no caller-supplied target tenant.
/// </summary>
public sealed class TenantExportService
{
    private readonly IAdministrationModule _administration;

    public TenantExportService(IAdministrationModule administration)
    {
        _administration = administration;
    }

    public Task<TenantExportDto> ExportAsync(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
        => _administration.ExportAsync(unitOfWork, cancellationToken);
}
