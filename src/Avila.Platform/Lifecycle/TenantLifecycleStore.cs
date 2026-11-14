using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Avila.Platform.Lifecycle;

public sealed class TenantLifecycleStore
{
    private readonly string _connectionString;

    public TenantLifecycleStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SetStatusAsync(
        Guid tenantId,
        string status,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (status is not ("active" or "suspended"))
        {
            throw new ArgumentException("Lifecycle status must be active or suspended.", nameof(status));
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);

        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(connection)
            .Options;

        await using var context = new PlatformDbContext(options);
        context.Database.UseTransaction(transaction);

        var lifecycle = await context.TenantLifecycles
            .SingleOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (lifecycle is null)
        {
            lifecycle = new TenantLifecycle
            {
                TenantId = tenantId,
                Status = status,
                PlanCode = "trial"
            };
            context.TenantLifecycles.Add(lifecycle);
        }
        else
        {
            lifecycle.Status = status;
        }

        lifecycle.SuspendedAt = status == "suspended" ? DateTimeOffset.UtcNow : null;
        lifecycle.SuspensionReason = status == "suspended" ? reason : null;
        lifecycle.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
