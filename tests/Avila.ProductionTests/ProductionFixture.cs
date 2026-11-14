using Avila.Modules.Administration;
using Avila.Modules.Billing;
using Avila.Modules.Inventory;
using Avila.Modules.Sales;
using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Avila.ProductionTests;

public sealed class ProductionFixture : IAsyncLifetime
{
    private readonly string _adminPassword = Guid.NewGuid().ToString("N");
    private readonly string _appPassword = Guid.NewGuid().ToString("N");
    private readonly string _identityPassword = Guid.NewGuid().ToString("N");
    private readonly string _platformPassword = Guid.NewGuid().ToString("N");
    private readonly PostgreSqlContainer _container;

    public ProductionFixture()
    {
        _container = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("avila")
            .WithUsername("postgres")
            .WithPassword(_adminPassword)
            .Build();
    }

    public string AdminConnectionString => _container.GetConnectionString();
    public string AppConnectionString { get; private set; } = string.Empty;
    public string IdentityConnectionString { get; private set; } = string.Empty;
    public string PlatformAdminConnectionString { get; private set; } = string.Empty;
    public string IdentityMfaKey { get; } = Convert.ToBase64String(
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    public static string RolesSqlPath => Path.Combine(
        AppContext.BaseDirectory,
        "ops",
        "postgres",
        "roles.sql");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using (var connection = new NpgsqlConnection(AdminConnectionString))
        {
            await connection.OpenAsync();
            await using var schemas = new NpgsqlCommand(
                "CREATE SCHEMA IF NOT EXISTS admin; CREATE SCHEMA IF NOT EXISTS inventory; CREATE SCHEMA IF NOT EXISTS sales; CREATE SCHEMA IF NOT EXISTS billing; CREATE SCHEMA IF NOT EXISTS platform; CREATE SCHEMA IF NOT EXISTS identity;",
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

        await using (var platform = new PlatformDbContext(Options<PlatformDbContext>(AdminConnectionString, "platform")))
        {
            await platform.Database.MigrateAsync();
        }

        await using (var identity = new IdentityDbContext(Options<IdentityDbContext>(AdminConnectionString, "identity")))
        {
            await identity.Database.MigrateAsync();
        }

        await using (var connection = new NpgsqlConnection(AdminConnectionString))
        {
            await connection.OpenAsync();
            var rolesSql = await File.ReadAllTextAsync(RolesSqlPath);
            await using var roles = new NpgsqlCommand(rolesSql, connection);
            await roles.ExecuteNonQueryAsync();

            // Test-only disposable credentials; generated hex, no user input.
            // The production roles.sql is password-free.
            await using var credentials = new NpgsqlCommand(
                $"""
                ALTER ROLE avila_app LOGIN PASSWORD '{_appPassword}';
                ALTER ROLE avila_identity LOGIN PASSWORD '{_identityPassword}';
                ALTER ROLE avila_platform LOGIN PASSWORD '{_platformPassword}';
                """,
                connection);
            await credentials.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Username = "avila_app",
            Password = _appPassword,
            MaxPoolSize = 5
        };
        AppConnectionString = builder.ConnectionString;

        var identityBuilder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Username = "avila_identity",
            Password = _identityPassword,
            MaxPoolSize = 5
        };
        IdentityConnectionString = identityBuilder.ConnectionString;

        var platformBuilder = new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Username = "avila_platform",
            Password = _platformPassword,
            MaxPoolSize = 5
        };
        PlatformAdminConnectionString = platformBuilder.ConnectionString;
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
