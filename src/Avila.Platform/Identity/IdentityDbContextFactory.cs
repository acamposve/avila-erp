using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Avila.Platform.Identity;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=avila;Username=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
            .Options;

        return new IdentityDbContext(options);
    }
}
