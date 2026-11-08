using Avila.Application;
using Avila.Modules.Administration;
using Avila.Modules.Billing;
using Avila.Modules.Inventory;
using Avila.Modules.Sales;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class SalesFlowTests
{
    private readonly ProductionFixture _fixture;

    public SalesFlowTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task R1_vertical_slice_creates_tenant_scoped_company_party_item_quote_order_and_draft()
    {
        var tenant = Guid.NewGuid();
        var service = CreateService();

        var result = await service.RunAsync(
            _fixture.AppConnectionString,
            tenant,
            new SalesFlowRequest(
                CompanyName: "Distribuidora Ávila",
                CompanyTaxId: "J-12345678-9",
                CustomerName: "Cliente Prueba",
                CustomerTaxId: "V-12345678",
                ItemSku: "SKU-001",
                ItemName: "Producto de prueba",
                UnitPrice: 10m,
                TaxRate: 0.16m,
                Quantity: 2m,
                Currency: "VES"));

        Assert.NotEqual(Guid.Empty, result.CompanyId);
        Assert.NotEqual(Guid.Empty, result.PartyId);
        Assert.NotEqual(Guid.Empty, result.ItemId);
        Assert.NotEqual(Guid.Empty, result.QuoteId);
        Assert.NotEqual(Guid.Empty, result.OrderId);
        Assert.NotEqual(Guid.Empty, result.InvoiceDraftId);
        Assert.Equal(20m, result.Subtotal);
        Assert.Equal(3.20m, result.TaxTotal);
        Assert.Equal(23.20m, result.Total);
        Assert.Equal("VES", result.Currency);

        Assert.Equal(1, await CountAsync("admin.companies", tenant));
        Assert.Equal(1, await CountAsync("admin.parties", tenant));
        Assert.Equal(1, await CountAsync("inventory.items", tenant));
        Assert.Equal(1, await CountAsync("sales.quotes", tenant));
        Assert.Equal(1, await CountAsync("sales.orders", tenant));
        Assert.Equal(1, await CountAsync("billing.invoice_drafts", tenant));
    }

    [Fact]
    public async Task R1_vertical_slice_keeps_tenants_isolated_through_rls()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var service = CreateService();

        await service.RunAsync(
            _fixture.AppConnectionString,
            tenantA,
            Request("A", "A-1", "SKU-A"));
        await service.RunAsync(
            _fixture.AppConnectionString,
            tenantB,
            Request("B", "B-1", "SKU-B"));

        await using var unitOfWork = await new TenantUnitOfWorkFactory()
            .BeginAsync(_fixture.AppConnectionString, tenantA);
        await using var context = new Avila.Modules.Sales.SalesDbContext(
            unitOfWork.OptionsFor<Avila.Modules.Sales.SalesDbContext>("sales"));
        unitOfWork.Enlist(context);

        var quotes = await context.Quotes.ToListAsync();
        Assert.Single(quotes);
        Assert.All(quotes, quote => Assert.Equal(tenantA, quote.TenantId));
    }

    private static SalesFlowService CreateService()
        => new(
            new AdministrationModule(),
            new InventoryModule(),
            new SalesModule(),
            new BillingModule(),
            new TenantUnitOfWorkFactory());

    private static SalesFlowRequest Request(string name, string taxId, string sku)
        => new(
            CompanyName: $"Company {name}",
            CompanyTaxId: taxId,
            CustomerName: $"Customer {name}",
            CustomerTaxId: $"V-{name}",
            ItemSku: sku,
            ItemName: $"Item {name}",
            UnitPrice: 10m,
            TaxRate: 0.16m,
            Quantity: 1m,
            Currency: "VES");

    private async Task<int> CountAsync(string table, Guid tenantId)
    {
        await using var connection = new Npgsql.NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            $"SELECT COUNT(*) FROM {table} WHERE tenant_id = @tenant",
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        return (int)(long)(await command.ExecuteScalarAsync() ?? 0L);
    }
}
