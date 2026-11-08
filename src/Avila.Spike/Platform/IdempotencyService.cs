using Avila.Spike.Tenancy;
using Npgsql;

namespace Avila.Spike.Platform;

public sealed record IdempotencyResult(string Response, bool Replayed);

public sealed class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException(string message) : base(message) { }
}

public sealed class IdempotencyInProgressException : Exception
{
    public IdempotencyInProgressException(string message) : base(message) { }
}

/// <summary>
/// Spike-only idempotency coordinator. The key is inserted in the same transaction as
/// the business action; concurrent duplicates wait on the primary key and then read
/// the completed response.
/// </summary>
public static class IdempotencyService
{
    public static async Task<IdempotencyResult> ExecuteAsync(
        string connectionString,
        Guid tenantId,
        string idempotencyKey,
        string operation,
        string requestHash,
        Func<NpgsqlConnection, NpgsqlTransaction, Task<string>> action,
        Func<Task>? onStarted = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);
        await TenantSession.SetLockTimeoutAsync(connection, transaction, cancellationToken: cancellationToken);

        try
        {
            await using var insert = new NpgsqlCommand(
                """
                INSERT INTO platform.idempotency_records
                    (tenant_id, idempotency_key, operation, request_hash, status, expires_at)
                VALUES
                    (@tenant, @key, @operation, @hash, 'in_progress', now() + interval '24 hours')
                """,
                connection,
                transaction);
            insert.Parameters.AddWithValue("tenant", tenantId);
            insert.Parameters.AddWithValue("key", idempotencyKey);
            insert.Parameters.AddWithValue("operation", operation);
            insert.Parameters.AddWithValue("hash", requestHash);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            return await ReadExistingAsync(
                connectionString,
                tenantId,
                idempotencyKey,
                operation,
                requestHash,
                cancellationToken);
        }

        if (onStarted is not null)
        {
            await onStarted();
        }

        var response = await action(connection, transaction);

        await using (var update = new NpgsqlCommand(
            """
            UPDATE platform.idempotency_records
            SET status = 'completed',
                status_code = 200,
                response_body = @response
            WHERE tenant_id = @tenant AND idempotency_key = @key AND operation = @operation
            """,
            connection,
            transaction))
        {
            update.Parameters.AddWithValue("response", response);
            update.Parameters.AddWithValue("tenant", tenantId);
            update.Parameters.AddWithValue("key", idempotencyKey);
            update.Parameters.AddWithValue("operation", operation);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new IdempotencyResult(response, Replayed: false);
    }

    private static async Task<IdempotencyResult> ReadExistingAsync(
        string connectionString,
        Guid tenantId,
        string idempotencyKey,
        string operation,
        string requestHash,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT request_hash, status, response_body
            FROM platform.idempotency_records
            WHERE tenant_id = @tenant AND idempotency_key = @key AND operation = @operation
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("key", idempotencyKey);
        command.Parameters.AddWithValue("operation", operation);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new IdempotencyConflictException(
                "The idempotency record disappeared before it could be read.");
        }

        var storedHash = reader.GetString(0);
        var status = reader.GetString(1);
        var response = reader.IsDBNull(2) ? null : reader.GetString(2);

        await reader.DisposeAsync();
        await transaction.CommitAsync(cancellationToken);

        if (!string.Equals(storedHash, requestHash, StringComparison.Ordinal))
        {
            throw new IdempotencyConflictException(
                "The idempotency key was reused with a different request hash.");
        }

        if (status != "completed" || response is null)
        {
            throw new IdempotencyInProgressException(
                "The request is still in progress; retry after the current attempt completes.");
        }

        return new IdempotencyResult(response, Replayed: true);
    }
}
