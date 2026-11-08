using Avila.Spike.Tenancy;
using Npgsql;
using NpgsqlTypes;

namespace Avila.Spike.Inventory;

public sealed record StockIssueRequest(
    Guid TenantId,
    Guid WarehouseId,
    Guid ItemId,
    decimal Quantity,
    Guid SourceId,
    Guid SourceLineId);

public sealed class StockInsufficientException : Exception
{
    public StockInsufficientException(string message) : base(message) { }
}

public sealed class StockBalanceNotFoundException : Exception
{
    public StockBalanceNotFoundException(string message) : base(message) { }
}

/// <summary>
/// Spike-only stock issue path. It proves the pessimistic lock from ADR-008:
/// one row is locked, checked, updated, and moved in the same transaction.
/// </summary>
public static class InventoryService
{
    public static async Task IssueAsync(
        string connectionString,
        StockIssueRequest request,
        Func<Task>? onLockAcquired = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, request.TenantId, cancellationToken);
        await TenantSession.SetLockTimeoutAsync(connection, transaction, cancellationToken: cancellationToken);

        decimal quantity;
        bool allowNegative;

        await using (var select = new NpgsqlCommand(
            """
            SELECT quantity, allow_negative
            FROM inventory.stock_balances
            WHERE tenant_id = @tenant AND warehouse_id = @warehouse AND item_id = @item
            FOR UPDATE
            """,
            connection,
            transaction))
        {
            select.Parameters.AddWithValue("tenant", request.TenantId);
            select.Parameters.AddWithValue("warehouse", request.WarehouseId);
            select.Parameters.AddWithValue("item", request.ItemId);

            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new StockBalanceNotFoundException(
                    $"No stock balance for tenant {request.TenantId}, warehouse {request.WarehouseId}, item {request.ItemId}.");
            }

            quantity = reader.GetDecimal(0);
            allowNegative = reader.GetBoolean(1);
        }

        if (onLockAcquired is not null)
        {
            await onLockAcquired();
        }

        if (!allowNegative && quantity < request.Quantity)
        {
            throw new StockInsufficientException(
                $"Insufficient stock: available {quantity}, requested {request.Quantity}.");
        }

        await using (var update = new NpgsqlCommand(
            """
            UPDATE inventory.stock_balances
            SET quantity = quantity - @quantity,
                updated_at = now()
            WHERE tenant_id = @tenant AND warehouse_id = @warehouse AND item_id = @item
            """,
            connection,
            transaction))
        {
            update.Parameters.Add(new NpgsqlParameter("quantity", NpgsqlDbType.Numeric) { Value = request.Quantity });
            update.Parameters.AddWithValue("tenant", request.TenantId);
            update.Parameters.AddWithValue("warehouse", request.WarehouseId);
            update.Parameters.AddWithValue("item", request.ItemId);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO inventory.stock_movements
                (id, tenant_id, warehouse_id, item_id, quantity, unit_cost,
                 source_type, source_id, source_line_id, movement_type)
            VALUES
                (@id, @tenant, @warehouse, @item, @quantity, 0,
                 'sale', @source, @source_line, 'issue')
            """,
            connection,
            transaction))
        {
            insert.Parameters.AddWithValue("id", Guid.NewGuid());
            insert.Parameters.AddWithValue("tenant", request.TenantId);
            insert.Parameters.AddWithValue("warehouse", request.WarehouseId);
            insert.Parameters.AddWithValue("item", request.ItemId);
            insert.Parameters.Add(new NpgsqlParameter("quantity", NpgsqlDbType.Numeric) { Value = -request.Quantity });
            insert.Parameters.AddWithValue("source", request.SourceId);
            insert.Parameters.AddWithValue("source_line", request.SourceLineId);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
