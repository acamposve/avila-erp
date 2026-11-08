using Microsoft.EntityFrameworkCore;

namespace Avila.Spike.Treasury;

public sealed class Receivable
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal AppliedAmount { get; set; }
    public string Currency { get; set; } = "VES";
    public string Status { get; set; } = "open";
}

public sealed class TreasuryDbContext : DbContext
{
    public TreasuryDbContext(DbContextOptions<TreasuryDbContext> options) : base(options)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    public DbSet<Receivable> Receivables => Set<Receivable>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Receivable>(entity =>
        {
            entity.ToTable("receivables", "treasury");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.CustomerId).HasColumnName("customer_id");
            entity.Property(x => x.OriginalAmount).HasColumnName("original_amount").HasPrecision(19, 4);
            entity.Property(x => x.AppliedAmount).HasColumnName("applied_amount").HasPrecision(19, 4);
            entity.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
            entity.Property(x => x.Status).HasColumnName("status");
        });
    }
}
