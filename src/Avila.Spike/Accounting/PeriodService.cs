using Avila.Spike.Tenancy;
using Npgsql;
using NpgsqlTypes;

namespace Avila.Spike.Accounting;

public sealed class PeriodClosedException : Exception
{
    public PeriodClosedException(string message) : base(message) { }
}

public sealed class PeriodNotFoundException : Exception
{
    public PeriodNotFoundException(string message) : base(message) { }
}

/// <summary>
/// Spike-only period posting/close path. Postings take a shared lock; close takes an
/// exclusive lock and moves the period through closing to closed.
/// </summary>
public static class PeriodService
{
    public static async Task PostJournalEntryAsync(
        string connectionString,
        Guid tenantId,
        Guid companyId,
        Guid periodId,
        Guid sourceId,
        decimal amount,
        Guid debitAccountId,
        Guid creditAccountId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);
        await TenantSession.SetLockTimeoutAsync(connection, transaction, cancellationToken: cancellationToken);

        var status = await ReadPeriodStatusAsync(
            connection,
            transaction,
            tenantId,
            companyId,
            periodId,
            forUpdate: false,
            cancellationToken);

        if (status != "open")
        {
            throw new PeriodClosedException($"Period {periodId} is not open (status: {status}).");
        }

        var entryId = Guid.NewGuid();
        await using (var entry = new NpgsqlCommand(
            """
            INSERT INTO accounting.journal_entries
                (id, tenant_id, posting_key, source_type, source_id, entry_type, status)
            VALUES
                (@id, @tenant, @posting_key, 'period-test', @source, 'original', 'posted')
            """,
            connection,
            transaction))
        {
            entry.Parameters.AddWithValue("id", entryId);
            entry.Parameters.AddWithValue("tenant", tenantId);
            entry.Parameters.AddWithValue("posting_key", $"{tenantId}:period-test:{sourceId}:1:original");
            entry.Parameters.AddWithValue("source", sourceId);
            await entry.ExecuteNonQueryAsync(cancellationToken);
        }

        await InsertLineAsync(connection, transaction, tenantId, entryId, debitAccountId, amount, credit: null, cancellationToken);
        await InsertLineAsync(connection, transaction, tenantId, entryId, creditAccountId, amount: null, credit: amount, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public static async Task CloseAsync(
        string connectionString,
        Guid tenantId,
        Guid companyId,
        Guid periodId,
        Func<Task>? onClosing = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);
        await TenantSession.SetLockTimeoutAsync(connection, transaction, cancellationToken: cancellationToken);

        var status = await ReadPeriodStatusAsync(
            connection,
            transaction,
            tenantId,
            companyId,
            periodId,
            forUpdate: true,
            cancellationToken);

        if (status != "open")
        {
            throw new PeriodClosedException($"Period {periodId} is not open (status: {status}).");
        }

        await SetStatusAsync(connection, transaction, tenantId, companyId, periodId, "closing", cancellationToken);

        if (onClosing is not null)
        {
            await onClosing();
        }

        await SetStatusAsync(connection, transaction, tenantId, companyId, periodId, "closed", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<string> ReadPeriodStatusAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid companyId,
        Guid periodId,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        var sql = forUpdate
            ? "SELECT status FROM accounting.periods WHERE tenant_id = @tenant AND company_id = @company AND period_id = @period FOR UPDATE"
            : "SELECT status FROM accounting.periods WHERE tenant_id = @tenant AND company_id = @company AND period_id = @period FOR SHARE";

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("period", periodId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as string
            ?? throw new PeriodNotFoundException($"Period {periodId} was not found for tenant {tenantId}.");
    }

    private static async Task SetStatusAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid companyId,
        Guid periodId,
        string status,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE accounting.periods
            SET status = @status
            WHERE tenant_id = @tenant AND company_id = @company AND period_id = @period
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("period", periodId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertLineAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid entryId,
        Guid accountId,
        decimal? amount,
        decimal? credit,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO accounting.journal_lines
                (id, tenant_id, journal_entry_id, account_id, debit, credit)
            VALUES
                (@id, @tenant, @entry, @account, @debit, @credit)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("entry", entryId);
        command.Parameters.AddWithValue("account", accountId);
        command.Parameters.Add(new NpgsqlParameter("debit", NpgsqlDbType.Numeric) { Value = amount ?? 0m });
        command.Parameters.Add(new NpgsqlParameter("credit", NpgsqlDbType.Numeric) { Value = credit ?? 0m });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
