using Avila.Modules.Administration;
using Avila.Modules.Billing;
using Avila.Modules.Inventory;
using Avila.Modules.Sales;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Avila.ProductionTests;

public sealed class ProductionFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("avila")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string AdminConnectionString => _container.GetConnectionString();
    public string AppConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using (var connection = new NpgsqlConnection(AdminConnectionString))
        {
            await connection.OpenAsync();
            await using var schemas = new NpgsqlCommand(
                "CREATE SCHEMA IF NOT EXISTS admin; CREATE SCHEMA IF NOT EXISTS inventory; CREATE SCHEMA IF NOT EXISTS sales; CREATE SCHEMA IF NOT EXISTS billing;",
                connection);
            await schemas.ExecuteNonQueryAsync();
        }

        await using (var admin = new AdministrationDbContext(Options<AdministrationDbContext>(AdminConnectionString, "admin")))
        {
            await admin.Database.MigrateAsync();
        }

        await using (var inventory = new InventoryDbContext(Options<InventoryDbContext>(AdminConnectionString, "inventory")))
        {
            await inventory.Database.MigrateAsync();
        }

        await using (var sales = new SalesDbContext(Options<SalesDbContext>(AdminConnectionString, "sales")))
        {
            await sales.Database.MigrateAsync();
        }

        await using (var billing = new BillingDbContext(Options<BillingDbContext>(AdminConnectionString, "billing")))
        {
            await billing.Database.MigrateAsync();
        }

        await using (var connection = new NpgsqlConnection(AdminConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                """
                CREATE ROLE avila_app LOGIN PASSWORD 'avila_app_pw' NOSUPERUSER NOBYPASSRLS;
                GRANT CONNECT ON DATABASE avila TO avila_app;
                GRANT USAGE ON SCHEMA admin, inventory, sales, billing TO avila_app;
                GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA admin, inventory, sales, billing TO avila_app;
                """,
                connection);
            await command.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Username = "avila_app",
            Password = "avila_app_pw",
            MaxPoolSize = 5
        };
        AppConnectionString = builder.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private static DbContextOptions<T> Options<T>(string connectionString, string schema) where T : DbContext
        => new DbContextOptionsBuilder<T>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", schema))
            .Options;
}

[CollectionDefinition("production-postgres")]
public sealed class ProductionCollection : ICollectionFixture<ProductionFixture>
{
}
