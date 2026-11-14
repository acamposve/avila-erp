using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class RlsPolicyTests
{
    private readonly ProductionFixture _fixture;

    public RlsPolicyTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Live_catalog_has_forced_rls_and_registered_policy_for_every_tenant_table()
    {
        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();

        var missingRegistry = await ScalarLongAsync(connection,
            """
            SELECT COUNT(*)
            FROM (
                SELECT c.table_schema, c.table_name
                FROM information_schema.columns c
                JOIN information_schema.tables t
                  ON t.table_schema = c.table_schema
                 AND t.table_name = c.table_name
                 AND t.table_type = 'BASE TABLE'
                WHERE c.column_name = 'tenant_id'
                  AND c.table_schema IN ('admin', 'inventory', 'sales', 'billing', 'platform')
            ) actual
            WHERE NOT EXISTS (
                SELECT 1
                FROM platform.rls_policy_registry r
                WHERE r.table_schema = actual.table_schema
                  AND r.table_name = actual.table_name
                  AND r.is_required
            )
            """);
        Assert.Equal(0L, missingRegistry);

        var missingPolicies = await ScalarLongAsync(connection,
            """
            SELECT COUNT(*)
            FROM platform.rls_policy_registry r
            JOIN pg_namespace n ON n.nspname = r.table_schema
            JOIN pg_class c ON c.relnamespace = n.oid AND c.relname = r.table_name
            WHERE r.is_required
              AND (
                  NOT c.relrowsecurity
                  OR NOT c.relforcerowsecurity
                  OR NOT EXISTS (
                      SELECT 1
                      FROM pg_policy p
                      WHERE p.polrelid = c.oid
                        AND p.polname = r.policy_name
                        AND pg_get_expr(p.polqual, p.polrelid) LIKE '%app.tenant_id%'
                  )
              )
            """);
        Assert.Equal(0L, missingPolicies);

        var registered = await ScalarLongAsync(
            connection,
            "SELECT COUNT(*) FROM platform.rls_policy_registry WHERE is_required");
        Assert.True(registered > 0);
        Assert.Equal(registered, await ScalarLongAsync(connection,
            """
            SELECT COUNT(*)
            FROM (
                SELECT c.table_schema, c.table_name
                FROM information_schema.columns c
                JOIN information_schema.tables t
                  ON t.table_schema = c.table_schema
                 AND t.table_name = c.table_name
                 AND t.table_type = 'BASE TABLE'
                WHERE c.column_name = 'tenant_id'
                  AND c.table_schema IN ('admin', 'inventory', 'sales', 'billing', 'platform')
            ) actual
            """));
    }

    private static async Task<long> ScalarLongAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }
}
