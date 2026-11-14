using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class RoleBootstrapTests
{
    private readonly ProductionFixture _fixture;

    public RoleBootstrapTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Roles_bootstrap_is_idempotent_and_least_privilege()
    {
        Assert.True(File.Exists(ProductionFixture.RolesSqlPath));
        var rolesSql = await File.ReadAllTextAsync(ProductionFixture.RolesSqlPath);

        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();

        for (var run = 0; run < 2; run++)
        {
            await using var command = new NpgsqlCommand(rolesSql, connection);
            await command.ExecuteNonQueryAsync();
        }

        var attributes = await QueryAsync(
            connection,
            """
            SELECT rolname, rolsuper, rolbypassrls, rolcreatedb, rolcreaterole
            FROM pg_roles
            WHERE rolname IN ('avila_app', 'avila_identity', 'avila_platform')
            ORDER BY rolname
            """);

        Assert.Equal(3, attributes.Count);
        foreach (var row in attributes)
        {
            Assert.Equal(false, row[1]);
            Assert.Equal(false, row[2]);
            Assert.Equal(false, row[3]);
            Assert.Equal(false, row[4]);
        }

        Assert.True(await HasTablePrivilegeAsync(connection, "avila_app", "admin.companies", "SELECT"));
        Assert.True(await HasTablePrivilegeAsync(connection, "avila_app", "platform.audit_events", "INSERT"));
        Assert.False(await HasTablePrivilegeAsync(connection, "avila_app", "platform.audit_events", "UPDATE"));
        Assert.False(await HasTablePrivilegeAsync(connection, "avila_app", "platform.audit_events", "DELETE"));
        Assert.False(await HasTablePrivilegeAsync(connection, "avila_app", "platform.tenants", "SELECT"));
        Assert.False(await HasTablePrivilegeAsync(connection, "avila_app", "identity.users", "SELECT"));

        Assert.True(await HasTablePrivilegeAsync(connection, "avila_identity", "identity.users", "SELECT"));
        Assert.False(await HasTablePrivilegeAsync(connection, "avila_identity", "admin.companies", "SELECT"));

        Assert.True(await HasTablePrivilegeAsync(connection, "avila_platform", "platform.tenants", "UPDATE"));
        Assert.True(await HasTablePrivilegeAsync(connection, "avila_platform", "platform.tenant_lifecycle", "INSERT"));
        Assert.False(await HasTablePrivilegeAsync(connection, "avila_platform", "admin.companies", "SELECT"));
    }

    private static async Task<bool> HasTablePrivilegeAsync(
        NpgsqlConnection connection,
        string role,
        string table,
        string privilege)
    {
        await using var command = new NpgsqlCommand(
            "SELECT has_table_privilege(@role, @table, @privilege)",
            connection);
        command.Parameters.AddWithValue("role", role);
        command.Parameters.AddWithValue("table", table);
        command.Parameters.AddWithValue("privilege", privilege);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private static async Task<List<object?[]>> QueryAsync(
        NpgsqlConnection connection,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<object?[]>();
        while (await reader.ReadAsync())
        {
            var values = new object?[reader.FieldCount];
            for (var index = 0; index < reader.FieldCount; index++)
            {
                values[index] = reader.GetValue(index);
            }

            rows.Add(values);
        }

        return rows;
    }
}
