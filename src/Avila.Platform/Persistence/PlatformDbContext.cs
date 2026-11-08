using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Persistence;

public sealed class TenantRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TenantSetting
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Platform persistence. Tenant settings are tenant-scoped and forced RLS;
/// the tenants table is global and must not be granted to the application role.
/// </summary>
public sealed class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    public DbSet<TenantRecord> Tenants => Set<TenantRecord>();
    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantRecord>(entity =>
        {
            entity.ToTable("tenants", "platform");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(24);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TenantSetting>(entity =>
        {
            entity.ToTable("tenant_settings", "platform");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.Key).HasColumnName("key").HasMaxLength(120);
            entity.Property(x => x.Value).HasColumnName("value").HasMaxLength(2000);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.Key }).IsUnique();
        });
    }
}
