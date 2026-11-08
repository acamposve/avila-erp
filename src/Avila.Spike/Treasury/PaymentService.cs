using Avila.Spike.Tenancy;
using Npgsql;
using NpgsqlTypes;

namespace Avila.Spike.Treasury;

public sealed class PaymentOverAllocatedException : Exception
{
    public PaymentOverAllocatedException(string message) : base(message) { }
}

public sealed class ReceivableNotFoundException : Exception
{
    public ReceivableNotFoundException(string message) : base(message) { }
}

/// <summary>
/// Spike-only payment application path. It proves the receivable row lock from ADR-008
/// and prevents two applications from exceeding the open balance.
/// </summary>
public static class PaymentService
{
    public static async Task ApplyAsync(
        string connectionString,
        Guid tenantId,
        Guid receivableId,
        Guid paymentId,
        decimal amount,
        Func<Task>? onLockAcquired = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);
        await TenantSession.SetLockTimeoutAsync(connection, transaction, cancellationToken: cancellationToken);

        decimal originalAmount;
        decimal appliedAmount;

        await using (var select = new NpgsqlCommand(
            """
            SELECT original_amount, applied_amount
            FROM treasury.receivables
            WHERE tenant_id = @tenant AND id = @id
            FOR UPDATE
            """,
            connection,
            transaction))
        {
            select.Parameters.AddWithValue("tenant", tenantId);
            select.Parameters.AddWithValue("id", receivableId);

            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new ReceivableNotFoundException($"Receivable {receivableId} was not found for tenant {tenantId}.");
            }

            originalAmount = reader.GetDecimal(0);
            appliedAmount = reader.GetDecimal(1);
        }

        if (onLockAcquired is not null)
        {
            await onLockAcquired();
        }

        if (appliedAmount + amount > originalAmount)
        {
            throw new PaymentOverAllocatedException(
                $"Payment {amount} would exceed receivable {receivableId}: applied {appliedAmount}, original {originalAmount}.");
        }

        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO treasury.payment_allocations
                (id, tenant_id, payment_id, receivable_id, amount, allocation_type)
            VALUES
                (@id, @tenant, @payment, @receivable, @amount, 'application')
            """,
            connection,
            transaction))
        {
            insert.Parameters.AddWithValue("id", Guid.NewGuid());
            insert.Parameters.AddWithValue("tenant", tenantId);
            insert.Parameters.AddWithValue("payment", paymentId);
            insert.Parameters.AddWithValue("receivable", receivableId);
            insert.Parameters.Add(new NpgsqlParameter("amount", NpgsqlDbType.Numeric) { Value = amount });
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var update = new NpgsqlCommand(
            """
            UPDATE treasury.receivables
            SET applied_amount = applied_amount + @amount,
                status = CASE
                    WHEN applied_amount + @amount >= original_amount THEN 'paid'
                    ELSE 'partial'
                END
            WHERE tenant_id = @tenant AND id = @id
            """,
            connection,
            transaction))
        {
            update.Parameters.Add(new NpgsqlParameter("amount", NpgsqlDbType.Numeric) { Value = amount });
            update.Parameters.AddWithValue("tenant", tenantId);
            update.Parameters.AddWithValue("id", receivableId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
