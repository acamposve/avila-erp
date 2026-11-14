using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avila.Platform.Tenancy;
using Npgsql;

namespace Avila.Platform.Idempotency;

public sealed record IdempotencyLease(
    Guid Id,
    string Key,
    string Operation,
    string RequestHash,
    bool IsReplay,
    string? ResponseJson,
    int? StatusCode);

public sealed class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

/// <summary>
/// Transactional idempotency ledger. The insert is part of the caller's
/// transaction; a concurrent duplicate waits on the unique key and then either
/// replays the committed response or reports a conflict.
/// </summary>
public static class IdempotencyService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<IdempotencyLease> BeginAsync(
        IUnitOfWork unitOfWork,
        string key,
        string operation,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(operation))
        {
            throw new IdempotencyConflictException("IDEMPOTENCY_INVALID", "Idempotency key and operation are required.");
        }

        var id = Guid.NewGuid();
        GuardLength(key, 200, "Idempotency key");
        GuardLength(operation, 120, "Idempotency operation");

        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO platform.idempotency_keys
                (id, tenant_id, key, operation, request_hash, status, created_at)
            VALUES
                (@id, @tenant, @key, @operation, @hash, 'in_progress', now())
            ON CONFLICT (tenant_id, operation, key) DO NOTHING
            RETURNING id
            """,
            unitOfWork.Connection,
            unitOfWork.Transaction))
        {
            insert.Parameters.AddWithValue("id", id);
            insert.Parameters.AddWithValue("tenant", unitOfWork.TenantId);
            insert.Parameters.AddWithValue("key", key);
            insert.Parameters.AddWithValue("operation", operation);
            insert.Parameters.AddWithValue("hash", requestHash);
            var inserted = await insert.ExecuteScalarAsync(cancellationToken);
            if (inserted is not null)
            {
                return new IdempotencyLease(id, key, operation, requestHash, false, null, null);
            }
        }

        await using var select = new NpgsqlCommand(
            """
            SELECT id, request_hash, status, response_json, status_code
            FROM platform.idempotency_keys
            WHERE tenant_id = @tenant
              AND operation = @operation
              AND key = @key
            """,
            unitOfWork.Connection,
            unitOfWork.Transaction);
        select.Parameters.AddWithValue("tenant", unitOfWork.TenantId);
        select.Parameters.AddWithValue("operation", operation);
        select.Parameters.AddWithValue("key", key);

        await using var reader = await select.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new IdempotencyConflictException("IDEMPOTENCY_MISSING", "Idempotency row disappeared.");
        }

        var existingHash = reader.GetString(1);
        if (!string.Equals(existingHash, requestHash, StringComparison.Ordinal))
        {
            throw new IdempotencyConflictException(
                "IDEMPOTENCY_KEY_REUSED",
                "The idempotency key was already used with a different request.");
        }

        var status = reader.GetString(2);
        if (status != "completed" || reader.IsDBNull(3))
        {
            throw new IdempotencyConflictException(
                "IDEMPOTENCY_IN_PROGRESS",
                "A request with this idempotency key is still in progress.");
        }

        return new IdempotencyLease(
            reader.GetGuid(0),
            key,
            operation,
            existingHash,
            true,
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetInt32(4));
    }

    public static async Task CompleteAsync<T>(
        IUnitOfWork unitOfWork,
        IdempotencyLease lease,
        T response,
        int statusCode,
        CancellationToken cancellationToken = default)
    {
        if (lease.IsReplay)
        {
            return;
        }

        var json = JsonSerializer.Serialize(response, JsonOptions);
        await using var update = new NpgsqlCommand(
            """
            UPDATE platform.idempotency_keys
            SET status = 'completed',
                response_json = CAST(@json AS jsonb),
                status_code = @status_code,
                completed_at = now()
            WHERE id = @id
              AND tenant_id = @tenant
              AND status = 'in_progress'
            """,
            unitOfWork.Connection,
            unitOfWork.Transaction);
        update.Parameters.AddWithValue("json", json);
        update.Parameters.AddWithValue("status_code", statusCode);
        update.Parameters.AddWithValue("id", lease.Id);
        update.Parameters.AddWithValue("tenant", unitOfWork.TenantId);

        var updated = await update.ExecuteNonQueryAsync(cancellationToken);
        if (updated != 1)
        {
            throw new IdempotencyConflictException(
                "IDEMPOTENCY_NOT_COMPLETED",
                "The idempotency row could not be completed.");
        }
    }

    public static string HashRequest<T>(T request)
        => Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request, JsonOptions))))
            .ToLowerInvariant();

    public static T DeserializeReplay<T>(IdempotencyLease lease)
        => JsonSerializer.Deserialize<T>(lease.ResponseJson!, JsonOptions)
           ?? throw new IdempotencyConflictException("IDEMPOTENCY_CORRUPT", "Stored response is invalid.");

    private static void GuardLength(string value, int max, string name)
    {
        if (value.Length > max)
        {
            throw new IdempotencyConflictException("IDEMPOTENCY_INVALID", $"{name} is too long.");
        }
    }
}
