using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Avila.Modules.Inventory;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=avila;Username=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "inventory"))
            .Options;

        return new InventoryDbContext(options);
    }
}
