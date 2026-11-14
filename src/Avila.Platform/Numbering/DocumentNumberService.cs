using Avila.Platform.Tenancy;
using Npgsql;

namespace Avila.Platform.Numbering;

public sealed record DocumentScope(string Type, Guid Id)
{
    public static DocumentScope Company(Guid id) => new("company", id);
    public static DocumentScope Branch(Guid id) => new("branch", id);
}

public static class DocumentNumberService
{
    public static async Task<string> ReserveAsync(
        IUnitOfWork unitOfWork,
        DocumentScope scope,
        string documentType,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        if (scope.Type is not ("company" or "branch"))
        {
            throw new ArgumentException("Document scope must be company or branch.", nameof(scope));
        }

        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Document type and prefix are required.");
        }

        await using (var insert = new NpgsqlCommand(
            """
            INSERT INTO platform.document_sequences
                (id, tenant_id, scope_type, scope_id, document_type, prefix, next_value, updated_at)
            VALUES
                (@id, @tenant, @scope_type, @scope_id, @document_type, @prefix, 1, now())
            ON CONFLICT (tenant_id, scope_type, scope_id, document_type) DO NOTHING
            """,
            unitOfWork.Connection,
            unitOfWork.Transaction))
        {
            insert.Parameters.AddWithValue("id", Guid.NewGuid());
            insert.Parameters.AddWithValue("tenant", unitOfWork.TenantId);
            insert.Parameters.AddWithValue("scope_type", scope.Type);
            insert.Parameters.AddWithValue("scope_id", scope.Id);
            insert.Parameters.AddWithValue("document_type", documentType);
            insert.Parameters.AddWithValue("prefix", prefix);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        long value;
        await using (var select = new NpgsqlCommand(
            """
            SELECT next_value
            FROM platform.document_sequences
            WHERE tenant_id = @tenant
              AND scope_type = @scope_type
              AND scope_id = @scope_id
              AND document_type = @document_type
            FOR UPDATE
            """,
            unitOfWork.Connection,
            unitOfWork.Transaction))
        {
            select.Parameters.AddWithValue("tenant", unitOfWork.TenantId);
            select.Parameters.AddWithValue("scope_type", scope.Type);
            select.Parameters.AddWithValue("scope_id", scope.Id);
            select.Parameters.AddWithValue("document_type", documentType);
            var result = await select.ExecuteScalarAsync(cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException("Document sequence row was not found after insert.");
            }

            value = (long)result;
        }

        await using (var update = new NpgsqlCommand(
            """
            UPDATE platform.document_sequences
            SET next_value = next_value + 1,
                updated_at = now()
            WHERE tenant_id = @tenant
              AND scope_type = @scope_type
              AND scope_id = @scope_id
              AND document_type = @document_type
            """,
            unitOfWork.Connection,
            unitOfWork.Transaction))
        {
            update.Parameters.AddWithValue("tenant", unitOfWork.TenantId);
            update.Parameters.AddWithValue("scope_type", scope.Type);
            update.Parameters.AddWithValue("scope_id", scope.Id);
            update.Parameters.AddWithValue("document_type", documentType);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        return $"{prefix}-{value:D8}";
    }
}
