using Avila.Spike.Tenancy;
using Npgsql;

namespace Avila.IntegrationTests;

internal static class TestDb
{
    public static async Task SeedStockAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid warehouseId,
        Guid itemId,
        decimal quantity,
        decimal averageCost = 0,
        bool allowNegative = false)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO inventory.stock_balances
                (tenant_id, warehouse_id, item_id, quantity, reserved, average_cost, allow_negative)
            VALUES
                (@tenant, @warehouse, @item, @quantity, 0, @average_cost, @allow_negative)
            """,
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("warehouse", warehouseId);
        command.Parameters.AddWithValue("item", itemId);
        command.Parameters.AddWithValue("quantity", quantity);
        command.Parameters.AddWithValue("average_cost", averageCost);
        command.Parameters.AddWithValue("allow_negative", allowNegative);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task SeedReceivableAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid receivableId,
        decimal originalAmount,
        Guid? invoiceId = null)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO treasury.receivables
                (id, tenant_id, customer_id, invoice_id, original_amount, applied_amount, currency, status)
            VALUES
                (@id, @tenant, @customer, @invoice, @original, 0, 'VES', 'open')
            """,
            connection);
        command.Parameters.AddWithValue("id", receivableId);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("customer", Guid.NewGuid());
        command.Parameters.AddWithValue("invoice", (object?)invoiceId ?? DBNull.Value);
        command.Parameters.AddWithValue("original", originalAmount);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<Guid> SeedInvoiceAsync(
        string adminConnectionString,
        Guid tenantId,
        long number = 1)
    {
        var id = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO billing.invoices
                (id, tenant_id, company_id, number, status)
            VALUES
                (@id, @tenant, @company, @number, 'issued')
            """,
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("company", Guid.NewGuid());
        command.Parameters.AddWithValue("number", number);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    public static async Task<decimal> GetStockAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid warehouseId,
        Guid itemId)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT quantity
            FROM inventory.stock_balances
            WHERE tenant_id = @tenant AND warehouse_id = @warehouse AND item_id = @item
            """,
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("warehouse", warehouseId);
        command.Parameters.AddWithValue("item", itemId);
        return (decimal)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Stock row was not found."));
    }

    public static async Task<int> CountMovementsAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid itemId)
    {
        return await ScalarAsync<int>(
            adminConnectionString,
            "SELECT COUNT(*) FROM inventory.stock_movements WHERE tenant_id = @tenant AND item_id = @item",
            ("tenant", tenantId),
            ("item", itemId));
    }

    public static async Task<decimal> GetAppliedAmountAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid receivableId)
    {
        return await ScalarAsync<decimal>(
            adminConnectionString,
            "SELECT applied_amount FROM treasury.receivables WHERE tenant_id = @tenant AND id = @id",
            ("tenant", tenantId),
            ("id", receivableId));
    }

    public static async Task<int> CountAllocationsAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid receivableId)
    {
        return await ScalarAsync<int>(
            adminConnectionString,
            "SELECT COUNT(*) FROM treasury.payment_allocations WHERE tenant_id = @tenant AND receivable_id = @id",
            ("tenant", tenantId),
            ("id", receivableId));
    }

    public static async Task<int> CountInvoicesAsync(string adminConnectionString, Guid tenantId)
    {
        return await ScalarAsync<int>(
            adminConnectionString,
            "SELECT COUNT(*) FROM billing.invoices WHERE tenant_id = @tenant",
            ("tenant", tenantId));
    }

    public static async Task<int> CountReceivablesAsync(string adminConnectionString, Guid tenantId)
    {
        return await ScalarAsync<int>(
            adminConnectionString,
            "SELECT COUNT(*) FROM treasury.receivables WHERE tenant_id = @tenant",
            ("tenant", tenantId));
    }

    public static async Task<(Guid[] Tenants, int BackendPid)> QueryInvoiceTenantsAsync(
        string appConnectionString,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(appConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (tenantId.HasValue)
        {
            await TenantSession.SetTenantAsync(connection, transaction, tenantId.Value, cancellationToken);
        }

        var tenants = new List<Guid>();
        int backendPid;

        await using (var command = new NpgsqlCommand(
            "SELECT DISTINCT tenant_id, pg_backend_pid() FROM billing.invoices ORDER BY tenant_id",
            connection,
            transaction))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                backendPid = reader.GetInt32(1);
                do
                {
                    tenants.Add(reader.GetGuid(0));
                }
                while (await reader.ReadAsync(cancellationToken));
            }
            else
            {
                backendPid = -1;
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return (tenants.Distinct().ToArray(), backendPid);
    }

    public static async Task<Guid> SeedAccountAsync(
        string adminConnectionString,
        Guid tenantId,
        string code,
        string name = "Spike account")
    {
        var id = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO accounting.accounts (id, tenant_id, code, name, is_postable)
            VALUES (@id, @tenant, @code, @name, true)
            """,
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("code", code);
        command.Parameters.AddWithValue("name", name);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    public static async Task<int> CountJournalEntriesAsync(string adminConnectionString, Guid tenantId)
    {
        return await ScalarAsync<int>(
            adminConnectionString,
            "SELECT COUNT(*) FROM accounting.journal_entries WHERE tenant_id = @tenant",
            ("tenant", tenantId));
    }

    public static async Task<int> CountJournalLinesAsync(string adminConnectionString, Guid tenantId)
    {
        return await ScalarAsync<int>(
            adminConnectionString,
            "SELECT COUNT(*) FROM accounting.journal_lines WHERE tenant_id = @tenant",
            ("tenant", tenantId));
    }

    public static async Task<Guid?> GetInvoiceJournalEntryIdAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid invoiceId)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT journal_entry_id FROM billing.invoices WHERE tenant_id = @tenant AND id = @id",
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("id", invoiceId);
        var result = await command.ExecuteScalarAsync();
        return result is DBNull or null ? null : (Guid)result;
    }

    public static async Task<(decimal Debit, decimal Credit)> GetJournalTotalsAsync(
        string adminConnectionString,
        Guid tenantId)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COALESCE(SUM(debit),0), COALESCE(SUM(credit),0) FROM accounting.journal_lines WHERE tenant_id = @tenant",
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetDecimal(0), reader.GetDecimal(1));
    }

    public static async Task<int> CountSequencesAsync(string adminConnectionString, Guid tenantId)
    {
        return await ScalarAsync<int>(
            adminConnectionString,
            "SELECT COUNT(*) FROM platform.document_sequences WHERE tenant_id = @tenant",
            ("tenant", tenantId));
    }

    public static async Task SeedTenantNoteAsync(
        string adminConnectionString,
        Guid tenantId,
        string note)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO platform.tenant_notes (id, tenant_id, note) VALUES (@id, @tenant, @note)",
            connection);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("note", note);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<(Guid[] Tenants, int BackendPid)> QueryTenantNotesAsync(
        string appConnectionString,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(appConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (tenantId.HasValue)
        {
            await TenantSession.SetTenantAsync(connection, transaction, tenantId.Value, cancellationToken);
        }

        var tenants = new List<Guid>();
        int backendPid;

        await using (var command = new NpgsqlCommand(
            "SELECT DISTINCT tenant_id, pg_backend_pid() FROM platform.tenant_notes ORDER BY tenant_id",
            connection,
            transaction))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                backendPid = reader.GetInt32(1);
                do
                {
                    tenants.Add(reader.GetGuid(0));
                }
                while (await reader.ReadAsync(cancellationToken));
            }
            else
            {
                backendPid = -1;
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return (tenants.Distinct().ToArray(), backendPid);
    }

    public static async Task<Exception?> TryInsertCrossTenantNoteAsync(
        string appConnectionString,
        Guid contextTenant,
        Guid noteTenant,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(appConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await TenantSession.SetTenantAsync(connection, transaction, contextTenant, cancellationToken);
            await using var command = new NpgsqlCommand(
                "INSERT INTO platform.tenant_notes (id, tenant_id, note) VALUES (@id, @tenant, 'cross-tenant')",
                connection,
                transaction);
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("tenant", noteTenant);
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return exception;
        }
    }

    public static async Task<Guid> SeedPeriodAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid companyId,
        string status = "open")
    {
        var id = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO accounting.periods (tenant_id, company_id, period_id, status)
            VALUES (@tenant, @company, @period, @status)
            """,
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("period", id);
        command.Parameters.AddWithValue("status", status);
        await command.ExecuteNonQueryAsync();
        return id;
    }

    public static async Task<string> GetPeriodStatusAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid companyId,
        Guid periodId)
    {
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT status
            FROM accounting.periods
            WHERE tenant_id = @tenant AND company_id = @company AND period_id = @period
            """,
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("period", periodId);
        return (string)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Period was not found."));
    }

    public static async Task<int> CountTenantNotesAsync(string adminConnectionString, Guid tenantId)
    {
        return await ScalarAsync<int>(
            adminConnectionString,
            "SELECT COUNT(*) FROM platform.tenant_notes WHERE tenant_id = @tenant",
            ("tenant", tenantId));
    }

    public static async Task<int> CountCutoverRunsAsync(string adminConnectionString, Guid tenantId)
    {
        return await ScalarAsync<int>(
            adminConnectionString,
            "SELECT COUNT(*) FROM accounting.cutover_runs WHERE tenant_id = @tenant",
            ("tenant", tenantId));
    }

    public static async Task<string> GetCutoverStatusAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid cutoverId)
    {
        return await ScalarAsync<string>(
            adminConnectionString,
            "SELECT status FROM accounting.cutover_runs WHERE tenant_id = @tenant AND id = @id",
            ("tenant", tenantId),
            ("id", cutoverId));
    }

    public static async Task<bool> IsAccountingEnabledAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid companyId)
    {
        return await ScalarAsync<bool>(
            adminConnectionString,
            "SELECT accounting_enabled FROM accounting.company_settings WHERE tenant_id = @tenant AND company_id = @company",
            ("tenant", tenantId),
            ("company", companyId));
    }

    public static async Task<int> CountOpeningLinksAsync(string adminConnectionString, Guid tenantId)
    {
        return await ScalarAsync<int>(
            adminConnectionString,
            "SELECT COUNT(*) FROM accounting.opening_links WHERE tenant_id = @tenant",
            ("tenant", tenantId));
    }

    public static async Task<string> GetInvoiceAccountingStatusAsync(
        string adminConnectionString,
        Guid tenantId,
        Guid invoiceId)
    {
        return await ScalarAsync<string>(
            adminConnectionString,
            "SELECT accounting_status FROM billing.invoices WHERE tenant_id = @tenant AND id = @id",
            ("tenant", tenantId),
            ("id", invoiceId));
    }

    public static async Task<(Guid[] Tenants, int BackendPid)> QueryInvoiceTenantsReadOnlyAsync(
        string appConnectionString,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(appConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var readOnly = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
        {
            await readOnly.ExecuteNonQueryAsync(cancellationToken);
        }

        if (tenantId.HasValue)
        {
            await TenantSession.SetTenantAsync(connection, transaction, tenantId.Value, cancellationToken);
        }

        var tenants = new List<Guid>();
        int backendPid;

        await using (var command = new NpgsqlCommand(
            "SELECT DISTINCT tenant_id, pg_backend_pid() FROM billing.invoices ORDER BY tenant_id",
            connection,
            transaction))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                backendPid = reader.GetInt32(1);
                do
                {
                    tenants.Add(reader.GetGuid(0));
                }
                while (await reader.ReadAsync(cancellationToken));
            }
            else
            {
                backendPid = -1;
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return (tenants.Distinct().ToArray(), backendPid);
    }

    private static async Task<T> ScalarAsync<T>(
        string connectionString,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        var result = await command.ExecuteScalarAsync();
        var scalar = result ?? throw new InvalidOperationException($"Query returned no scalar: {sql}");
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(scalar, targetType, System.Globalization.CultureInfo.InvariantCulture);
    }
}
