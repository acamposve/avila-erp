using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Lifecycle;

public sealed class TenantSuspendedException : Exception
{
    public TenantSuspendedException(Guid tenantId)
        : base($"Tenant {tenantId} is suspended; writes are blocked.")
    {
        TenantId = tenantId;
    }

    public Guid TenantId { get; }
}

public sealed class TenantAccessGuard
{
    public async Task EnsureWritesAllowedAsync(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var lifecycle = await context.TenantLifecycles
            .SingleOrDefaultAsync(x => x.TenantId == unitOfWork.TenantId, cancellationToken);

        if (lifecycle?.Status == "suspended")
        {
            throw new TenantSuspendedException(unitOfWork.TenantId);
        }
    }
}

public sealed class TenantFeatureService
{
    public async Task SetAsync(
        IUnitOfWork unitOfWork,
        string key,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Feature flag key is required.", nameof(key));
        }

        await using var context = CreateContext(unitOfWork);
        var flag = await context.FeatureFlags
            .SingleOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (flag is null)
        {
            context.FeatureFlags.Add(new FeatureFlag
            {
                Id = Guid.NewGuid(),
                TenantId = unitOfWork.TenantId,
                Key = key,
                Enabled = enabled
            });
        }
        else
        {
            flag.Enabled = enabled;
            flag.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsEnabledAsync(
        IUnitOfWork unitOfWork,
        string key,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext(unitOfWork);
        var flag = await context.FeatureFlags
            .SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        return flag?.Enabled ?? false;
    }

    private static PlatformDbContext CreateContext(IUnitOfWork unitOfWork)
    {
        var context = new PlatformDbContext(unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);
        return context;
    }
}

public sealed class LimitExceededException : Exception
{
    public LimitExceededException(string resource, long limit)
        : base($"Limit exceeded for {resource}; limit={limit}.")
    {
        Resource = resource;
        Limit = limit;
    }

    public string Resource { get; }
    public long Limit { get; }
}

public sealed class TenantUsageService
{
    public async Task ConfigureLimitAsync(
        IUnitOfWork unitOfWork,
        string resource,
        long limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource) || limit < 0)
        {
            throw new ArgumentException("Resource and non-negative limit are required.");
        }

        await using var context = CreateContext(unitOfWork);
        var counter = await context.UsageCounters
            .SingleOrDefaultAsync(x => x.Resource == resource, cancellationToken);

        if (counter is null)
        {
            context.UsageCounters.Add(new UsageCounter
            {
                Id = Guid.NewGuid(),
                TenantId = unitOfWork.TenantId,
                Resource = resource,
                LimitValue = limit
            });
        }
        else
        {
            counter.LimitValue = limit;
            counter.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> ConsumeAsync(
        IUnitOfWork unitOfWork,
        string resource,
        long amount = 1,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        }

        await using var context = CreateContext(unitOfWork);
        var counter = await context.UsageCounters
            .SingleOrDefaultAsync(x => x.Resource == resource, cancellationToken);

        if (counter is null)
        {
            throw new InvalidOperationException($"No usage limit is configured for {resource}.");
        }

        if (counter.Used + amount > counter.LimitValue)
        {
            throw new LimitExceededException(resource, counter.LimitValue);
        }

        counter.Used += amount;
        counter.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return counter.Used;
    }

    private static PlatformDbContext CreateContext(IUnitOfWork unitOfWork)
    {
        var context = new PlatformDbContext(unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);
        return context;
    }
}
