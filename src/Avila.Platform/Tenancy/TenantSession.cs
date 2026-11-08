using Npgsql;

namespace Avila.Platform.Tenancy;

public static class TenantSession
{
    public static async Task SetTenantAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var command = new NpgsqlCommand(
            "SELECT set_config('app.tenant_id', @tenant, true)",
            connection,
            transaction);
        command.Parameters.AddWithValue("tenant", tenantId.ToString());
        await command.ExecuteScalarAsync(cancellationToken);
    }

    public static async Task SetLockTimeoutAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string timeout = "5s",
        CancellationToken cancellationToken = default)
    {
        await using var command = new NpgsqlCommand(
            $"SET LOCAL lock_timeout = '{timeout}'",
            connection,
            transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
