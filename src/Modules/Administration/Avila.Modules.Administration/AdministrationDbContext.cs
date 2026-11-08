using Microsoft.EntityFrameworkCore;

namespace Avila.Modules.Administration;

public sealed class Company
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Party
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public bool IsCustomer { get; set; }
    public bool IsSupplier { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AdministrationDbContext : DbContext
{
    public AdministrationDbContext(DbContextOptions<AdministrationDbContext> options) : base(options)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Party> Parties => Set<Party>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies", "admin");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(x => x.TaxId).HasColumnName("tax_id").HasMaxLength(40);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.TaxId }).IsUnique();
        });

        modelBuilder.Entity<Party>(entity =>
        {
            entity.ToTable("parties", "admin");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(x => x.TaxId).HasColumnName("tax_id").HasMaxLength(40);
            entity.Property(x => x.IsCustomer).HasColumnName("is_customer");
            entity.Property(x => x.IsSupplier).HasColumnName("is_supplier");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.TaxId }).IsUnique();
        });
    }
}
