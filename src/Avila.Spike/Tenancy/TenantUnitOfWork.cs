using Avila.Spike.Billing;
using Avila.Spike.Treasury;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Avila.Spike.Tenancy;

/// <summary>
/// One connection, one transaction, two module contexts. This is the spike's proof of
/// the shared-unit-of-work model in ADR-007. It is not the final production coordinator.
/// </summary>
public sealed class TenantUnitOfWork : IAsyncDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private bool _committed;

    public BillingDbContext Billing { get; }
    public TreasuryDbContext Treasury { get; }

    internal NpgsqlConnection Connection => _connection;
    internal NpgsqlTransaction Transaction => _transaction;

    private TenantUnitOfWork(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        BillingDbContext billing,
        TreasuryDbContext treasury)
    {
        _connection = connection;
        _transaction = transaction;
        Billing = billing;
        Treasury = treasury;
    }

    public static async Task<TenantUnitOfWork> BeginAsync(
        string connectionString,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);
        await TenantSession.SetLockTimeoutAsync(connection, transaction, cancellationToken: cancellationToken);

        var billing = CreateBilling(connection);
        billing.Database.UseTransaction(transaction);

        var treasury = CreateTreasury(connection);
        treasury.Database.UseTransaction(transaction);

        return new TenantUnitOfWork(connection, transaction, billing, treasury);
    }

    private static BillingDbContext CreateBilling(NpgsqlConnection connection)
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql(connection)
            .Options;
        return new BillingDbContext(options);
    }

    private static TreasuryDbContext CreateTreasury(NpgsqlConnection connection)
    {
        var options = new DbContextOptionsBuilder<TreasuryDbContext>()
            .UseNpgsql(connection)
            .Options;
        return new TreasuryDbContext(options);
    }

    public async Task<int> SaveBillingAsync(CancellationToken cancellationToken = default)
        => await Billing.SaveChangesAsync(cancellationToken);

    public async Task<int> SaveTreasuryAsync(CancellationToken cancellationToken = default)
        => await Treasury.SaveChangesAsync(cancellationToken);

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
        _committed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.RollbackAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Billing.DisposeAsync();
        await Treasury.DisposeAsync();

        if (!_committed)
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            catch (InvalidOperationException)
            {
                // The transaction may already have been rolled back explicitly.
            }
        }

        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
