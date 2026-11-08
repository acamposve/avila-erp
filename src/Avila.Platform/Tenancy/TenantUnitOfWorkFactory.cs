using Avila.Platform.Tenancy;

namespace Avila.Platform.Tenancy;

public sealed class TenantUnitOfWorkFactory : IUnitOfWorkFactory
{
    public async Task<IUnitOfWork> BeginAsync(
        string connectionString,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await TenantUnitOfWork.BeginAsync(connectionString, tenantId, cancellationToken);
    }
}
