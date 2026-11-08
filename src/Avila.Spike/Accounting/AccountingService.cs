using Npgsql;
using NpgsqlTypes;

namespace Avila.Spike.Accounting;

public sealed class AccountingPostingException : Exception
{
    public AccountingPostingException(string message) : base(message) { }
}

public sealed class AccountingDuplicatePostingException : Exception
{
    public AccountingDuplicatePostingException(string message) : base(message) { }
}

/// <summary>
/// Spike-only synchronous posting for an invoice. The caller must already be inside the
/// tenant unit of work; this method deliberately does not own the transaction.
/// </summary>
public static class AccountingService
{
    public static async Task<Guid> PostInvoiceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid invoiceId,
        decimal amount,
        Guid debitAccountId,
        Guid creditAccountId,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new AccountingPostingException("Invoice amount must be positive.");
        }

        await EnsurePostableAccountAsync(connection, transaction, tenantId, debitAccountId, cancellationToken);
        await EnsurePostableAccountAsync(connection, transaction, tenantId, creditAccountId, cancellationToken);

        var entryId = Guid.NewGuid();
        var postingKey = $"{tenantId}:invoice:{invoiceId}:1:original";

        try
        {
            await using (var entry = new NpgsqlCommand(
                """
                INSERT INTO accounting.journal_entries
                    (id, tenant_id, posting_key, source_type, source_id, entry_type, status)
                VALUES
                    (@id, @tenant, @posting_key, 'invoice', @invoice, 'original', 'posted')
                """,
                connection,
                transaction))
            {
                entry.Parameters.AddWithValue("id", entryId);
                entry.Parameters.AddWithValue("tenant", tenantId);
                entry.Parameters.AddWithValue("posting_key", postingKey);
                entry.Parameters.AddWithValue("invoice", invoiceId);
                await entry.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new AccountingDuplicatePostingException(
                $"An accounting entry already exists for invoice {invoiceId}.");
        }

        await using (var debit = new NpgsqlCommand(
            """
            INSERT INTO accounting.journal_lines
                (id, tenant_id, journal_entry_id, account_id, debit, credit)
            VALUES
                (@id, @tenant, @entry, @account, @amount, 0)
            """,
            connection,
            transaction))
        {
            debit.Parameters.AddWithValue("id", Guid.NewGuid());
            debit.Parameters.AddWithValue("tenant", tenantId);
            debit.Parameters.AddWithValue("entry", entryId);
            debit.Parameters.AddWithValue("account", debitAccountId);
            debit.Parameters.Add(new NpgsqlParameter("amount", NpgsqlDbType.Numeric) { Value = amount });
            await debit.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var credit = new NpgsqlCommand(
            """
            INSERT INTO accounting.journal_lines
                (id, tenant_id, journal_entry_id, account_id, debit, credit)
            VALUES
                (@id, @tenant, @entry, @account, 0, @amount)
            """,
            connection,
            transaction))
        {
            credit.Parameters.AddWithValue("id", Guid.NewGuid());
            credit.Parameters.AddWithValue("tenant", tenantId);
            credit.Parameters.AddWithValue("entry", entryId);
            credit.Parameters.AddWithValue("account", creditAccountId);
            credit.Parameters.Add(new NpgsqlParameter("amount", NpgsqlDbType.Numeric) { Value = amount });
            await credit.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var link = new NpgsqlCommand(
            """
            UPDATE billing.invoices
            SET journal_entry_id = @entry
            WHERE tenant_id = @tenant AND id = @invoice
            """,
            connection,
            transaction))
        {
            link.Parameters.AddWithValue("entry", entryId);
            link.Parameters.AddWithValue("tenant", tenantId);
            link.Parameters.AddWithValue("invoice", invoiceId);
            await link.ExecuteNonQueryAsync(cancellationToken);
        }

        return entryId;
    }

    private static async Task EnsurePostableAccountAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM accounting.accounts
            WHERE tenant_id = @tenant AND id = @account AND is_postable = true
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("account", accountId);

        var count = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        if (count != 1)
        {
            throw new AccountingPostingException(
                $"Account {accountId} is missing or not postable for tenant {tenantId}.");
        }
    }
}
