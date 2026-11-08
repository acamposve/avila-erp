using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Avila.Platform.Persistence;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=avila;Username=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "platform"))
            .Options;

        return new PlatformDbContext(options);
    }
}
