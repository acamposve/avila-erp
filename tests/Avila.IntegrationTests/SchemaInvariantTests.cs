using Npgsql;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class SchemaInvariantTests
{
    private readonly PostgresFixture _fixture;

    public SchemaInvariantTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T09b_every_tenant_table_is_forced_rls_with_a_policy()
    {
        var tenantTables = new List<(string Schema, string Table)>();

        await using (var connection = new NpgsqlConnection(_fixture.AdminConnectionString))
        {
            await connection.OpenAsync();

            await using (var columns = new NpgsqlCommand(
                """
                SELECT table_schema, table_name
                FROM information_schema.columns
                WHERE column_name = 'tenant_id'
                  AND table_schema IN ('platform', 'billing', 'treasury', 'inventory', 'accounting')
                ORDER BY table_schema, table_name
                """,
                connection))
            await using (var reader = await columns.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    tenantTables.Add((reader.GetString(0), reader.GetString(1)));
                }
            }

            Assert.NotEmpty(tenantTables);

            foreach (var (schema, table) in tenantTables)
            {
                await using var rls = new NpgsqlCommand(
                    """
                    SELECT c.relrowsecurity, c.relforcerowsecurity
                    FROM pg_class c
                    JOIN pg_namespace n ON n.oid = c.relnamespace
                    WHERE n.nspname = @schema AND c.relname = @table
                    """,
                    connection);
                rls.Parameters.AddWithValue("schema", schema);
                rls.Parameters.AddWithValue("table", table);

                await using var reader = await rls.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync(), $"Table {schema}.{table} was not found.");
                Assert.True(reader.GetBoolean(0), $"RLS not enabled on {schema}.{table}.");
                Assert.True(reader.GetBoolean(1), $"RLS not forced on {schema}.{table}.");
                await reader.DisposeAsync();

                await using var policies = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM pg_policies WHERE schemaname = @schema AND tablename = @table",
                    connection);
                policies.Parameters.AddWithValue("schema", schema);
                policies.Parameters.AddWithValue("table", table);
                var policyCount = (long)(await policies.ExecuteScalarAsync() ?? 0L);
                Assert.True(policyCount > 0, $"No RLS policy on {schema}.{table}.");
            }

            await using var role = new NpgsqlCommand(
                "SELECT rolbypassrls, rolsuper FROM pg_roles WHERE rolname = 'avila_app'",
                connection);
            await using var roleReader = await role.ExecuteReaderAsync();
            Assert.True(await roleReader.ReadAsync());
            Assert.False(roleReader.GetBoolean(0));
            Assert.False(roleReader.GetBoolean(1));
        }
    }
}
