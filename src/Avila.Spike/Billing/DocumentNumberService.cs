using Avila.Spike.Tenancy;
using Npgsql;

namespace Avila.Spike.Billing;

/// <summary>
/// Spike-only numbering path. It uses a row lock in platform.document_sequences and
/// runs inside the caller's transaction. This is not the final production service.
/// </summary>
public static class DocumentNumberService
{
    public static async Task<long> NextAsync(
        string connectionString,
        Guid tenantId,
        Guid companyId,
        string scopeType,
        Guid scopeId,
        string documentType,
        string series,
        int fiscalYear,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);
        await TenantSession.SetLockTimeoutAsync(connection, transaction, cancellationToken: cancellationToken);

        var value = await NextAsync(
            connection,
            transaction,
            tenantId,
            companyId,
            scopeType,
            scopeId,
            documentType,
            series,
            fiscalYear,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return value;
    }

    internal static async Task<long> NextAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid companyId,
        string scopeType,
        Guid scopeId,
        string documentType,
        string series,
        int fiscalYear,
        CancellationToken cancellationToken)
    {
        if (scopeType is not ("company" or "branch"))
        {
            throw new ArgumentException("Scope type must be 'company' or 'branch'.", nameof(scopeType));
        }

        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO platform.document_sequences
                (tenant_id, company_id, scope_type, scope_id, document_type, series, fiscal_year, next_value)
            VALUES
                (@tenant, @company, @scope_type, @scope_id, @document_type, @series, @year, 1)
            ON CONFLICT DO NOTHING
            """,
            connection,
            transaction))
        {
            insert.Parameters.AddWithValue("tenant", tenantId);
            insert.Parameters.AddWithValue("company", companyId);
            insert.Parameters.AddWithValue("scope_type", scopeType);
            insert.Parameters.AddWithValue("scope_id", scopeId);
            insert.Parameters.AddWithValue("document_type", documentType);
            insert.Parameters.AddWithValue("series", series);
            insert.Parameters.AddWithValue("year", fiscalYear);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        long value;

        await using (var select = new NpgsqlCommand(
            """
            SELECT next_value
            FROM platform.document_sequences
            WHERE tenant_id = @tenant
              AND company_id = @company
              AND scope_type = @scope_type
              AND scope_id = @scope_id
              AND document_type = @document_type
              AND series = @series
              AND fiscal_year = @year
            FOR UPDATE
            """,
            connection,
            transaction))
        {
            select.Parameters.AddWithValue("tenant", tenantId);
            select.Parameters.AddWithValue("company", companyId);
            select.Parameters.AddWithValue("scope_type", scopeType);
            select.Parameters.AddWithValue("scope_id", scopeId);
            select.Parameters.AddWithValue("document_type", documentType);
            select.Parameters.AddWithValue("series", series);
            select.Parameters.AddWithValue("year", fiscalYear);

            value = (long)(await select.ExecuteScalarAsync(cancellationToken)
                ?? throw new InvalidOperationException("Document sequence row was not found."));
        }

        await using (var update = new NpgsqlCommand(
            """
            UPDATE platform.document_sequences
            SET next_value = next_value + 1,
                updated_at = now()
            WHERE tenant_id = @tenant
              AND company_id = @company
              AND scope_type = @scope_type
              AND scope_id = @scope_id
              AND document_type = @document_type
              AND series = @series
              AND fiscal_year = @year
            """,
            connection,
            transaction))
        {
            update.Parameters.AddWithValue("tenant", tenantId);
            update.Parameters.AddWithValue("company", companyId);
            update.Parameters.AddWithValue("scope_type", scopeType);
            update.Parameters.AddWithValue("scope_id", scopeId);
            update.Parameters.AddWithValue("document_type", documentType);
            update.Parameters.AddWithValue("series", series);
            update.Parameters.AddWithValue("year", fiscalYear);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        return value;
    }
}
