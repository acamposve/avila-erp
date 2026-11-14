using Avila.Platform.Lifecycle;
using Avila.Platform.Tenancy;

namespace Avila.Platform.Jobs;

/// <summary>
/// Ambient tenant state is never a source of truth. The job boundary clears it
/// before execution; jobs receive an explicit tenant on the UoW.
/// </summary>
public static class TenantExecutionContext
{
    private static readonly AsyncLocal<Guid?> Current = new();

    public static Guid? TenantId => Current.Value;

    public static void Set(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id cannot be empty.", nameof(tenantId));
        }

        Current.Value = tenantId;
    }

    public static void Clear() => Current.Value = null;
}

public interface ITenantJob
{
    Task ExecuteAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken = default);
}

public interface ITenantJobRunner
{
    Task RunAsync(
        Guid tenantId,
        ITenantJob job,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Runs one job in one tenant-scoped UoW. The tenant id is explicit; empty
/// tenants and null jobs are rejected at the boundary.
/// </summary>
public sealed class TenantJobRunner : ITenantJobRunner
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly string _connectionString;
    private readonly TenantAccessGuard _accessGuard;

    public TenantJobRunner(
        IUnitOfWorkFactory factory,
        string connectionString,
        TenantAccessGuard? accessGuard = null)
    {
        _factory = factory;
        _connectionString = connectionString;
        _accessGuard = accessGuard ?? new TenantAccessGuard();
    }

    public Task RunAsync(
        Guid tenantId,
        ITenantJob job,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("A non-empty tenant id is required.", nameof(tenantId));
        }

        ArgumentNullException.ThrowIfNull(job);

        // Clear synchronously at the public boundary, before returning the task.
        // This prevents the caller's ambient context from flowing into the job.
        TenantExecutionContext.Clear();
        return RunCoreAsync(tenantId, job, cancellationToken);
    }

    private async Task RunCoreAsync(
        Guid tenantId,
        ITenantJob job,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var unitOfWork = await _factory.BeginAsync(
                _connectionString,
                tenantId,
                cancellationToken);

            await _accessGuard.EnsureWritesAllowedAsync(unitOfWork, cancellationToken);
            await job.ExecuteAsync(unitOfWork, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        finally
        {
            TenantExecutionContext.Clear();
        }
    }
}
