using Avila.Modules.Inventory;
using Avila.Modules.Inventory.Contracts;
using Avila.Platform.Exporting;
using Avila.Platform.Tenancy;
using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class InventoryCatalogTests
{
    private readonly ProductionFixture _fixture;

    public InventoryCatalogTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Catalog_is_tenant_scoped_exported_and_rls_registered()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await using (var unit = await new TenantUnitOfWorkFactory()
                         .BeginAsync(_fixture.AppConnectionString, tenantA))
        {
            var module = new InventoryModule();
            var category = await module.CreateCategoryAsync(unit, new CreateCategoryCommand("Hardware"));
            var uom = await module.CreateUnitOfMeasureAsync(unit, new CreateUnitOfMeasureCommand("EA", "Each"));
            var warehouse = await module.CreateWarehouseAsync(
                unit, new CreateWarehouseCommand("MAIN", "Main", "Caracas"));
            var item = await module.CreateItemAsync(
                unit,
                new CreateItemCommand(
                    "SKU-1",
                    "Item One",
                    12.345678m,
                    0m,
                    false,
                    "SYNTHETIC-CLASS",
                    category.Id,
                    uom.Id));
            await module.CreateBinLocationAsync(
                unit, new CreateBinLocationCommand(warehouse.Id, "A1", "Aisle 1"));
            await module.CreateVariantAsync(
                unit, new CreateItemVariantCommand(item.Id, "SKU-1-RED", "Red"));

            var export = await new InventoryExportContributor().ExportAsync(
                unit,
                new TenantExportContext(tenantA));
            Assert.Equal("inventory", export.ModuleKey);
            Assert.Contains(export.Records, x => x.Entity == "item");
            Assert.Contains(export.Records, x => x.Entity == "warehouse");
            Assert.Contains(export.Records, x => x.Entity == "bin_location");
            Assert.Contains(export.Records, x => x.Entity == "item_variant");
            var itemRecord = export.Records.Single(x => x.Entity == "item");
            Assert.Equal("SYNTHETIC-CLASS", itemRecord.Data.GetProperty("tax_classification_code").GetString());
            Assert.Equal(12.345678m, itemRecord.Data.GetProperty("unit_price").GetDecimal());
            await unit.CommitAsync();
        }

        await using (var unitB = await new TenantUnitOfWorkFactory()
                         .BeginAsync(_fixture.AppConnectionString, tenantB))
        {
            var exportB = await new InventoryExportContributor().ExportAsync(
                unitB,
                new TenantExportContext(tenantB));
            Assert.Empty(exportB.Records);
            await unitB.RollbackAsync();
        }

        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*) FROM platform.rls_policy_registry
            WHERE table_schema = 'inventory'
              AND table_name IN ('categories','warehouses','unit_of_measures','item_variants','bin_locations')
              AND policy_name = 'tenant_isolation'
            """,
            connection);
        Assert.Equal(5L, (long)(await command.ExecuteScalarAsync() ?? 0L));
    }
}
