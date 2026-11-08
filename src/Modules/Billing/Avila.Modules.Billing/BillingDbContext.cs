using Microsoft.EntityFrameworkCore;

namespace Avila.Modules.Billing;

public sealed class InvoiceDraft
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid OrderId { get; set; }
    public Guid PartyId { get; set; }
    public string Status { get; set; } = "draft";
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "VES";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<InvoiceDraftLine> Lines { get; set; } = [];
}

public sealed class InvoiceDraftLine
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid InvoiceDraftId { get; set; }
    public Guid ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    public DbSet<InvoiceDraft> InvoiceDrafts => Set<InvoiceDraft>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvoiceDraft>(entity =>
        {
            entity.ToTable("invoice_drafts", "billing");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.CompanyId).HasColumnName("company_id");
            entity.Property(x => x.OrderId).HasColumnName("order_id");
            entity.Property(x => x.PartyId).HasColumnName("party_id");
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(24);
            entity.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(19, 4);
            entity.Property(x => x.TaxTotal).HasColumnName("tax_total").HasPrecision(19, 4);
            entity.Property(x => x.Total).HasColumnName("total").HasPrecision(19, 4);
            entity.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.OrderId }).IsUnique();
            entity.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.InvoiceDraftId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceDraftLine>(entity =>
        {
            entity.ToTable("invoice_draft_lines", "billing");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.InvoiceDraftId).HasColumnName("invoice_draft_id");
            entity.Property(x => x.ItemId).HasColumnName("item_id");
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(240);
            entity.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(19, 6);
            entity.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(19, 4);
            entity.Property(x => x.TaxRate).HasColumnName("tax_rate").HasPrecision(9, 6);
            entity.Property(x => x.LineTotal).HasColumnName("line_total").HasPrecision(19, 4);
        });
    }
}
