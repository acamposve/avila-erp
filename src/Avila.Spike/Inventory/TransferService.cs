using Avila.Spike.Tenancy;
using Npgsql;
using NpgsqlTypes;

namespace Avila.Spike.Inventory;

/// <summary>
/// Spike-only transfer path. It locks both balances in warehouse-id order, which is
/// the deterministic lock order described in ADR-008.
/// </summary>
public static class TransferService
{
    public static async Task TransferAsync(
        string connectionString,
        Guid tenantId,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        Guid itemId,
        decimal quantity,
        Guid transferId,
        CancellationToken cancellationToken = default)
    {
        if (fromWarehouseId == toWarehouseId)
        {
            throw new ArgumentException("Source and destination warehouses must differ.");
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);
        await TenantSession.SetLockTimeoutAsync(connection, transaction, cancellationToken: cancellationToken);

        var first = fromWarehouseId.CompareTo(toWarehouseId) < 0 ? fromWarehouseId : toWarehouseId;
        var second = first == fromWarehouseId ? toWarehouseId : fromWarehouseId;

        var firstQuantity = await LockBalanceAsync(connection, transaction, tenantId, first, itemId, cancellationToken);
        var secondQuantity = await LockBalanceAsync(connection, transaction, tenantId, second, itemId, cancellationToken);

        var fromQuantity = fromWarehouseId == first ? firstQuantity : secondQuantity;

        if (fromQuantity < quantity)
        {
            throw new StockInsufficientException(
                $"Insufficient stock for transfer: available {fromQuantity}, requested {quantity}.");
        }

        await UpdateBalanceAsync(connection, transaction, tenantId, fromWarehouseId, itemId, -quantity, cancellationToken);
        await UpdateBalanceAsync(connection, transaction, tenantId, toWarehouseId, itemId, quantity, cancellationToken);

        await InsertMovementAsync(
            connection, transaction, tenantId, fromWarehouseId, itemId, -quantity,
            transferId, fromWarehouseId, "transfer_out", cancellationToken);
        await InsertMovementAsync(
            connection, transaction, tenantId, toWarehouseId, itemId, quantity,
            transferId, toWarehouseId, "transfer_in", cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<decimal> LockBalanceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid warehouseId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT quantity
            FROM inventory.stock_balances
            WHERE tenant_id = @tenant AND warehouse_id = @warehouse AND item_id = @item
            FOR UPDATE
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("warehouse", warehouseId);
        command.Parameters.AddWithValue("item", itemId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null
            ? throw new StockBalanceNotFoundException($"No balance in warehouse {warehouseId}.")
            : (decimal)result;
    }

    private static async Task UpdateBalanceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid warehouseId,
        Guid itemId,
        decimal delta,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE inventory.stock_balances
            SET quantity = quantity + @delta,
                updated_at = now()
            WHERE tenant_id = @tenant AND warehouse_id = @warehouse AND item_id = @item
            """,
            connection,
            transaction);
        command.Parameters.Add(new NpgsqlParameter("delta", NpgsqlDbType.Numeric) { Value = delta });
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("warehouse", warehouseId);
        command.Parameters.AddWithValue("item", itemId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertMovementAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid warehouseId,
        Guid itemId,
        decimal quantity,
        Guid transferId,
        Guid sourceLineId,
        string movementType,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO inventory.stock_movements
                (id, tenant_id, warehouse_id, item_id, quantity, unit_cost,
                 source_type, source_id, source_line_id, movement_type)
            VALUES
                (@id, @tenant, @warehouse, @item, @quantity, 0,
                 'transfer', @source, @source_line, @movement_type)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("warehouse", warehouseId);
        command.Parameters.AddWithValue("item", itemId);
        command.Parameters.Add(new NpgsqlParameter("quantity", NpgsqlDbType.Numeric) { Value = quantity });
        command.Parameters.AddWithValue("source", transferId);
        command.Parameters.AddWithValue("source_line", sourceLineId);
        command.Parameters.AddWithValue("movement_type", movementType);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
