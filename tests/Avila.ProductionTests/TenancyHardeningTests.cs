using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class TenancyHardeningTests
{
    private readonly ProductionFixture _fixture;

    public TenancyHardeningTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Platform_tenant_settings_are_isolated_and_backfill_is_per_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await CreateTenantAsync(tenantA, "Tenant A");
        await CreateTenantAsync(tenantB, "Tenant B");

        var backfill = new TenantSettingsBackfill(new TenantUnitOfWorkFactory());
        var created = await backfill.SeedAsync(
            _fixture.AppConnectionString,
            [tenantA, tenantB],
            "locale",
            "es-VE");

        Assert.Equal(2, created);

        var rowsA = await ReadSettingsAsync(tenantA);
        var rowsB = await ReadSettingsAsync(tenantB);
        Assert.Single(rowsA);
        Assert.Single(rowsB);
        Assert.All(rowsA, row => Assert.Equal(tenantA, row.TenantId));
        Assert.All(rowsB, row => Assert.Equal(tenantB, row.TenantId));
        Assert.Equal(2, await CountAsync("platform.tenant_settings", tenantId: null));
    }

    [Fact]
    public async Task App_role_cannot_read_global_platform_tenants_table()
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM platform.tenants", connection);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            async () => await command.ExecuteScalarAsync());

        Assert.Equal("42501", exception.SqlState);
    }

    [Fact]
    public async Task Pool_reuse_does_not_leave_tenant_context()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var connectionString = WithMaxPoolSizeOne(_fixture.AppConnectionString);
        var factory = new TenantUnitOfWorkFactory();

        await using (var unitOfWork = await factory.BeginAsync(connectionString, tenantA))
        {
            Assert.Equal(tenantA.ToString(), await CurrentTenantAsync(unitOfWork));
            await unitOfWork.CommitAsync();
        }

        var noContext = await ScalarAsync(connectionString, "SELECT current_setting('app.tenant_id', true)");
        Assert.True(string.IsNullOrEmpty(noContext));

        await using (var unitOfWork = await factory.BeginAsync(connectionString, tenantB))
        {
            Assert.Equal(tenantB.ToString(), await CurrentTenantAsync(unitOfWork));
            await unitOfWork.CommitAsync();
        }
    }

    [Fact]
    public async Task Injected_failure_rolls_back_all_module_rows()
    {
        var tenant = Guid.NewGuid();
        var factory = new TenantUnitOfWorkFactory();
        var module = new AdministrationModule();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var unitOfWork = await factory.BeginAsync(_fixture.AppConnectionString, tenant);
            await module.CreateCompanyAsync(
                unitOfWork,
                new CreateCompanyCommand("Rollback Co", "J-ROLLBACK-1"));
            throw new InvalidOperationException("Injected failure after module write.");
        });

        Assert.Equal(0, await CountAsync("admin.companies", tenant));
    }

    [Fact]
    public async Task App_role_without_tenant_context_sees_no_rows()
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();

        await using var companies = new NpgsqlCommand("SELECT COUNT(*) FROM admin.companies", connection);
        await using var settings = new NpgsqlCommand("SELECT COUNT(*) FROM platform.tenant_settings", connection);

        Assert.Equal(0L, (long)(await companies.ExecuteScalarAsync() ?? 0L));
        Assert.Equal(0L, (long)(await settings.ExecuteScalarAsync() ?? 0L));
    }

    [Fact]
    public async Task Rls_introspection_covers_all_tenant_tables()
    {
        var rows = await ReadRlsRowsAsync();

        Assert.NotEmpty(rows);
        Assert.All(rows, row =>
        {
            Assert.True(row.RowSecurity, $"{row.Schema}.{row.Table} has RLS disabled.");
            Assert.True(row.ForceRowSecurity, $"{row.Schema}.{row.Table} does not force RLS.");
            Assert.True(row.HasPolicy, $"{row.Schema}.{row.Table} has no policy.");
        });

        Assert.Contains(rows, row => row.Schema == "platform" && row.Table == "tenant_settings");

        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var roleCommand = new NpgsqlCommand(
            "SELECT rolsuper, rolbypassrls FROM pg_roles WHERE rolname = 'avila_app'",
            connection);
        await using var reader = await roleCommand.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.False(reader.GetBoolean(0));
        Assert.False(reader.GetBoolean(1));
        await reader.CloseAsync();

        await using var globalCommand = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = 'platform'
              AND table_name = 'tenants'
              AND column_name = 'tenant_id'
            """,
            connection);
        Assert.Equal(0L, (long)(await globalCommand.ExecuteScalarAsync() ?? 0L));
    }

    private async Task CreateTenantAsync(Guid tenantId, string name)
    {
        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO platform.tenants (id, name, status, created_at)
            VALUES (@id, @name, 'active', now())
            """,
            connection);
        command.Parameters.AddWithValue("id", tenantId);
        command.Parameters.AddWithValue("name", name);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<TenantSetting>> ReadSettingsAsync(Guid tenantId)
    {
        var factory = new TenantUnitOfWorkFactory();
        await using var unitOfWork = await factory.BeginAsync(_fixture.AppConnectionString, tenantId);
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);
        return await context.TenantSettings.ToListAsync();
    }

    private async Task<int> CountAsync(string table, Guid? tenantId)
    {
        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        var where = tenantId.HasValue ? " WHERE tenant_id = @tenant" : string.Empty;
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {table}{where}", connection);
        if (tenantId.HasValue)
        {
            command.Parameters.AddWithValue("tenant", tenantId.Value);
        }

        return (int)(long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private static async Task<string?> CurrentTenantAsync(IUnitOfWork unitOfWork)
    {
        await using var command = new NpgsqlCommand(
            "SELECT current_setting('app.tenant_id', true)",
            unitOfWork.Connection,
            unitOfWork.Transaction);
        return (string?)await command.ExecuteScalarAsync();
    }

    private static async Task<string?> ScalarAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (string?)await command.ExecuteScalarAsync();
    }

    private static string WithMaxPoolSizeOne(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            MaxPoolSize = 1
        };
        return builder.ConnectionString;
    }

    private async Task<List<RlsRow>> ReadRlsRowsAsync()
    {
        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT n.nspname,
                   c.relname,
                   c.relrowsecurity,
                   c.relforcerowsecurity,
                   EXISTS (
                       SELECT 1 FROM pg_policy p WHERE p.polrelid = c.oid
                   ) AS has_policy
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname IN ('admin', 'inventory', 'sales', 'billing', 'platform')
              AND c.relkind = 'r'
              AND EXISTS (
                  SELECT 1
                  FROM information_schema.columns col
                  WHERE col.table_schema = n.nspname
                    AND col.table_name = c.relname
                    AND col.column_name = 'tenant_id'
              )
            """,
            connection);

        var rows = new List<RlsRow>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new RlsRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetBoolean(2),
                reader.GetBoolean(3),
                reader.GetBoolean(4)));
        }

        return rows;
    }

    private sealed record RlsRow(
        string Schema,
        string Table,
        bool RowSecurity,
        bool ForceRowSecurity,
        bool HasPolicy);
}
