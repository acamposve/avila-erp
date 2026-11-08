using Microsoft.EntityFrameworkCore;

namespace Avila.Modules.Sales;

public sealed class Quote
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PartyId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "VES";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<QuoteLine> Lines { get; set; } = [];
}

public sealed class QuoteLine
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid QuoteId { get; set; }
    public Guid ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class Order
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PartyId { get; set; }
    public Guid QuoteId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Status { get; set; } = "confirmed";
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "VES";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderLine> Lines { get; set; } = [];
}

public sealed class OrderLine
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quote>(entity =>
        {
            entity.ToTable("quotes", "sales");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.CompanyId).HasColumnName("company_id");
            entity.Property(x => x.PartyId).HasColumnName("party_id");
            entity.Property(x => x.Number).HasColumnName("number").HasMaxLength(48);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(24);
            entity.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(19, 4);
            entity.Property(x => x.TaxTotal).HasColumnName("tax_total").HasPrecision(19, 4);
            entity.Property(x => x.Total).HasColumnName("total").HasPrecision(19, 4);
            entity.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.QuoteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuoteLine>(entity =>
        {
            entity.ToTable("quote_lines", "sales");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.QuoteId).HasColumnName("quote_id");
            entity.Property(x => x.ItemId).HasColumnName("item_id");
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(240);
            entity.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(19, 6);
            entity.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(19, 4);
            entity.Property(x => x.TaxRate).HasColumnName("tax_rate").HasPrecision(9, 6);
            entity.Property(x => x.LineTotal).HasColumnName("line_total").HasPrecision(19, 4);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders", "sales");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.CompanyId).HasColumnName("company_id");
            entity.Property(x => x.PartyId).HasColumnName("party_id");
            entity.Property(x => x.QuoteId).HasColumnName("quote_id");
            entity.Property(x => x.Number).HasColumnName("number").HasMaxLength(48);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(24);
            entity.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(19, 4);
            entity.Property(x => x.TaxTotal).HasColumnName("tax_total").HasPrecision(19, 4);
            entity.Property(x => x.Total).HasColumnName("total").HasPrecision(19, 4);
            entity.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.ToTable("order_lines", "sales");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.OrderId).HasColumnName("order_id");
            entity.Property(x => x.ItemId).HasColumnName("item_id");
            entity.Property(x => x.Description).HasColumnName("description").HasMaxLength(240);
            entity.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(19, 6);
            entity.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(19, 4);
            entity.Property(x => x.TaxRate).HasColumnName("tax_rate").HasPrecision(9, 6);
            entity.Property(x => x.LineTotal).HasColumnName("line_total").HasPrecision(19, 4);
        });
    }
}
