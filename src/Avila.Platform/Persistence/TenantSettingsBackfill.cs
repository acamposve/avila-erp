using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Persistence;

/// <summary>
/// Per-tenant backfill: each tenant runs in its own UoW and transaction, so
/// RLS remains active and no connection-level tenant state is shared.
/// </summary>
public sealed class TenantSettingsBackfill
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public TenantSettingsBackfill(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<int> SeedAsync(
        string connectionString,
        IEnumerable<Guid> tenantIds,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        var created = 0;

        foreach (var tenantId in tenantIds.Distinct())
        {
            await using var unitOfWork = await _unitOfWorkFactory.BeginAsync(
                connectionString,
                tenantId,
                cancellationToken);

            await using var context = new PlatformDbContext(
                unitOfWork.OptionsFor<PlatformDbContext>("platform"));
            unitOfWork.Enlist(context);

            var exists = await context.TenantSettings
                .AnyAsync(x => x.Key == key, cancellationToken);

            if (!exists)
            {
                context.TenantSettings.Add(new TenantSetting
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Key = key,
                    Value = value
                });
                await context.SaveChangesAsync(cancellationToken);
                created++;
            }

            await unitOfWork.CommitAsync(cancellationToken);
        }

        return created;
    }
}
