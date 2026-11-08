using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Avila.Modules.Sales;

public sealed class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=avila;Username=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "sales"))
            .Options;

        return new SalesDbContext(options);
    }
}
