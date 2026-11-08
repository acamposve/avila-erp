using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Avila.Modules.Administration;

public sealed class AdministrationDbContextFactory : IDesignTimeDbContextFactory<AdministrationDbContext>
{
    public AdministrationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AdministrationDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=avila;Username=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "admin"))
            .Options;

        return new AdministrationDbContext(options);
    }
}
