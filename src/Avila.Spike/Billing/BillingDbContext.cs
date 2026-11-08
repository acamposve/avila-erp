using Microsoft.EntityFrameworkCore;

namespace Avila.Spike.Billing;

public sealed class Invoice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public long Number { get; set; }
    public string Status { get; set; } = "issued";
    public string AccountingStatus { get; set; } = "not_applicable";
    public Guid? JournalEntryId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices", "billing");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.CompanyId).HasColumnName("company_id");
            entity.Property(x => x.Number).HasColumnName("number");
            entity.Property(x => x.Status).HasColumnName("status");
            entity.Property(x => x.AccountingStatus).HasColumnName("accounting_status");
            entity.Property(x => x.JournalEntryId).HasColumnName("journal_entry_id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.CompanyId, x.Number }).IsUnique();
        });
    }
}
