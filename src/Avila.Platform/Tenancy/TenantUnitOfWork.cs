using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Avila.Platform.Tenancy;

public interface IUnitOfWork : IAsyncDisposable
{
    Guid TenantId { get; }
    NpgsqlConnection Connection { get; }
    NpgsqlTransaction Transaction { get; }

    DbContextOptions<T> OptionsFor<T>(string migrationSchema) where T : DbContext;
    void Enlist(DbContext context);

    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWorkFactory
{
    Task<IUnitOfWork> BeginAsync(
        string connectionString,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// One PostgreSQL connection and transaction per use case. Module contexts are
/// enlisted into the same transaction; the caller owns commit/rollback.
/// </summary>
public sealed class TenantUnitOfWork : IUnitOfWork
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private bool _committed;

    private TenantUnitOfWork(Guid tenantId, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        TenantId = tenantId;
        _connection = connection;
        _transaction = transaction;
    }

    public Guid TenantId { get; }
    public NpgsqlConnection Connection => _connection;
    public NpgsqlTransaction Transaction => _transaction;

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

        return new TenantUnitOfWork(tenantId, connection, transaction);
    }

    public DbContextOptions<T> OptionsFor<T>(string migrationSchema) where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseNpgsql(
                _connection,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", migrationSchema))
            .Options;
    }

    public void Enlist(DbContext context)
    {
        context.Database.UseTransaction(_transaction);
    }

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
        if (!_committed)
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            catch (InvalidOperationException)
            {
                // The transaction may already be rolled back.
            }
        }

        await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
