using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Avila.Api.Observability;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly string? _connectionString;

    public DatabaseHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Platform")
                            ?? configuration.GetConnectionString("Tenant");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return HealthCheckResult.Degraded("Database connection is not configured.");
        }

        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Database is not reachable.",
                exception);
        }
    }
}
