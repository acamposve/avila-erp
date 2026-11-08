using Avila.Spike.Tenancy;
using Npgsql;
using NpgsqlTypes;

namespace Avila.Spike.Accounting;

/// <summary>
/// Spike-only R2→R3 cutover. It creates opening entries for open receivables and
/// inventory, links the source items, then enables accounting for the company.
/// It is intentionally not a production migration or a full accounting cutover.
/// </summary>
public static class CutoverService
{
    public static async Task<Guid> RunAsync(
        string connectionString,
        Guid tenantId,
        Guid companyId,
        DateOnly cutoverDate,
        Guid receivableAccountId,
        Guid inventoryAccountId,
        Guid openingEquityAccountId,
        Guid? requestedCutoverId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);
        await TenantSession.SetLockTimeoutAsync(connection, transaction, cancellationToken: cancellationToken);

        var existing = await ReadSettingsAsync(connection, transaction, tenantId, companyId, cancellationToken);
        if (existing is { Enabled: true, CutoverId: not null })
        {
            return existing.Value.CutoverId.Value;
        }

        var cutoverId = requestedCutoverId ?? Guid.NewGuid();

        await using (var insertSettings = new NpgsqlCommand(
            """
            INSERT INTO accounting.company_settings (tenant_id, company_id, accounting_enabled, cutover_id)
            VALUES (@tenant, @company, false, NULL)
            ON CONFLICT DO NOTHING
            """,
            connection,
            transaction))
        {
            insertSettings.Parameters.AddWithValue("tenant", tenantId);
            insertSettings.Parameters.AddWithValue("company", companyId);
            await insertSettings.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insertRun = new NpgsqlCommand(
            """
            INSERT INTO accounting.cutover_runs
                (id, tenant_id, company_id, cutover_date, status)
            VALUES
                (@id, @tenant, @company, @date, 'prepared')
            """,
            connection,
            transaction))
        {
            insertRun.Parameters.AddWithValue("id", cutoverId);
            insertRun.Parameters.AddWithValue("tenant", tenantId);
            insertRun.Parameters.AddWithValue("company", companyId);
            insertRun.Parameters.AddWithValue("date", cutoverDate);
            await insertRun.ExecuteNonQueryAsync(cancellationToken);
        }

        var receivableTotal = await ReadReceivableTotalAsync(connection, transaction, tenantId, cancellationToken);
        var inventoryTotal = await ReadInventoryTotalAsync(connection, transaction, tenantId, cancellationToken);

        var receivableEntryId = Guid.Empty;
        if (receivableTotal > 0)
        {
            receivableEntryId = await InsertOpeningEntryAsync(
                connection,
                transaction,
                tenantId,
                cutoverId,
                "receivable",
                receivableTotal,
                receivableAccountId,
                openingEquityAccountId,
                cancellationToken);
        }

        if (inventoryTotal > 0)
        {
            await InsertOpeningEntryAsync(
                connection,
                transaction,
                tenantId,
                cutoverId,
                "inventory",
                inventoryTotal,
                inventoryAccountId,
                openingEquityAccountId,
                cancellationToken);
        }

        await using (var links = new NpgsqlCommand(
            """
            INSERT INTO accounting.opening_links
                (id, tenant_id, cutover_id, source_type, source_id, journal_entry_id, amount)
            SELECT gen_random_uuid(), @tenant, @cutover, 'receivable', r.id, @entry,
                   (r.original_amount - r.applied_amount)
            FROM treasury.receivables r
            WHERE r.tenant_id = @tenant
              AND r.original_amount > r.applied_amount
            """,
            connection,
            transaction))
        {
            links.Parameters.AddWithValue("tenant", tenantId);
            links.Parameters.AddWithValue("cutover", cutoverId);
            links.Parameters.AddWithValue("entry", receivableEntryId);
            await links.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var linkInvoices = new NpgsqlCommand(
            """
            UPDATE billing.invoices i
            SET accounting_status = 'opening_linked'
            WHERE i.tenant_id = @tenant
              AND EXISTS (
                  SELECT 1
                  FROM treasury.receivables r
                  WHERE r.tenant_id = i.tenant_id
                    AND r.invoice_id = i.id
                    AND r.original_amount > r.applied_amount
              )
            """,
            connection,
            transaction))
        {
            linkInvoices.Parameters.AddWithValue("tenant", tenantId);
            await linkInvoices.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var reconcile = new NpgsqlCommand(
            """
            UPDATE accounting.cutover_runs
            SET status = 'active', activated_at = now()
            WHERE tenant_id = @tenant AND id = @id
            """,
            connection,
            transaction))
        {
            reconcile.Parameters.AddWithValue("tenant", tenantId);
            reconcile.Parameters.AddWithValue("id", cutoverId);
            await reconcile.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var enable = new NpgsqlCommand(
            """
            UPDATE accounting.company_settings
            SET accounting_enabled = true, cutover_id = @cutover
            WHERE tenant_id = @tenant AND company_id = @company
            """,
            connection,
            transaction))
        {
            enable.Parameters.AddWithValue("cutover", cutoverId);
            enable.Parameters.AddWithValue("tenant", tenantId);
            enable.Parameters.AddWithValue("company", companyId);
            await enable.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return cutoverId;
    }

    private static async Task<(bool Enabled, Guid? CutoverId)?> ReadSettingsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT accounting_enabled, cutover_id FROM accounting.company_settings WHERE tenant_id = @tenant AND company_id = @company",
            connection,
            transaction);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("company", companyId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return (reader.GetBoolean(0), reader.IsDBNull(1) ? null : reader.GetGuid(1));
    }

    private static async Task<decimal> ReadReceivableTotalAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT COALESCE(SUM(original_amount - applied_amount), 0) FROM treasury.receivables WHERE tenant_id = @tenant AND original_amount > applied_amount",
            connection,
            transaction);
        command.Parameters.AddWithValue("tenant", tenantId);
        return (decimal)(await command.ExecuteScalarAsync(cancellationToken) ?? 0m);
    }

    private static async Task<decimal> ReadInventoryTotalAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT COALESCE(SUM(quantity * average_cost), 0) FROM inventory.stock_balances WHERE tenant_id = @tenant",
            connection,
            transaction);
        command.Parameters.AddWithValue("tenant", tenantId);
        return (decimal)(await command.ExecuteScalarAsync(cancellationToken) ?? 0m);
    }

    private static async Task<Guid> InsertOpeningEntryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid cutoverId,
        string sourceType,
        decimal amount,
        Guid debitAccountId,
        Guid creditAccountId,
        CancellationToken cancellationToken)
    {
        var entryId = Guid.NewGuid();

        await using (var entry = new NpgsqlCommand(
            """
            INSERT INTO accounting.journal_entries
                (id, tenant_id, posting_key, source_type, source_id, entry_type, status)
            VALUES
                (@id, @tenant, @posting_key, 'cutover', @cutover, 'opening', 'posted')
            """,
            connection,
            transaction))
        {
            entry.Parameters.AddWithValue("id", entryId);
            entry.Parameters.AddWithValue("tenant", tenantId);
            entry.Parameters.AddWithValue("posting_key", $"{tenantId}:cutover:{cutoverId}:{sourceType}:opening");
            entry.Parameters.AddWithValue("cutover", cutoverId);
            await entry.ExecuteNonQueryAsync(cancellationToken);
        }

        await InsertLineAsync(connection, transaction, tenantId, entryId, debitAccountId, debit: amount, credit: 0, cancellationToken);
        await InsertLineAsync(connection, transaction, tenantId, entryId, creditAccountId, debit: 0, credit: amount, cancellationToken);

        return entryId;
    }

    private static async Task InsertLineAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid entryId,
        Guid accountId,
        decimal debit,
        decimal credit,
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
        command.Parameters.Add(new NpgsqlParameter("debit", NpgsqlDbType.Numeric) { Value = debit });
        command.Parameters.Add(new NpgsqlParameter("credit", NpgsqlDbType.Numeric) { Value = credit });
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
