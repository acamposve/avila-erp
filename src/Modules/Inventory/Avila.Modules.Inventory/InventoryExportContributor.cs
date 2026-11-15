using System.Text.Json;
using Avila.Platform.Exporting;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.Modules.Inventory;

public sealed class InventoryExportContributor : ITenantExportContributor
{
    public string ModuleKey => "inventory";
    public int Order => 20;
    public int SchemaVersion => 1;

    public async Task<TenantExportSection> ExportAsync(
        IUnitOfWork unitOfWork,
        TenantExportContext context,
        CancellationToken cancellationToken = default)
    {
        await using var db = new InventoryDbContext(
            unitOfWork.OptionsFor<InventoryDbContext>("inventory"));
        unitOfWork.Enlist(db);

        var records = new List<TenantExportRecord>();
        var categories = await db.Categories.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        records.AddRange(categories.Select(x => new TenantExportRecord(
            "category", x.Id.ToString("D"),
            JsonSerializer.SerializeToElement(new { x.Name, x.ParentCode, x.Version }))));

        var uoms = await db.UnitOfMeasures.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        records.AddRange(uoms.Select(x => new TenantExportRecord(
            "unit_of_measure", x.Id.ToString("D"),
            JsonSerializer.SerializeToElement(new { x.Code, x.Name, x.Version }))));

        var warehouses = await db.Warehouses.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        records.AddRange(warehouses.Select(x => new TenantExportRecord(
            "warehouse", x.Id.ToString("D"),
            JsonSerializer.SerializeToElement(new { x.Code, x.Name, x.Address, x.IsActive, x.Version }))));

        var bins = await db.BinLocations.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        records.AddRange(bins.Select(x => new TenantExportRecord(
            "bin_location", x.Id.ToString("D"),
            JsonSerializer.SerializeToElement(new { x.WarehouseId, x.Code, x.Name, x.Version }))));

        var items = await db.Items.OrderBy(x => x.Sku).ToListAsync(cancellationToken);
        records.AddRange(items.Select(x => new TenantExportRecord(
            "item", x.Id.ToString("D"),
            JsonSerializer.SerializeToElement(new
            {
                x.Sku,
                x.Name,
                unit_price = x.UnitPrice,
                is_service = x.IsService,
                tax_classification_code = x.TaxClassificationCode,
                category_id = x.CategoryId,
                unit_of_measure_id = x.UnitOfMeasureId,
                x.Version
            }))));

        var variants = await db.ItemVariants.OrderBy(x => x.Sku).ToListAsync(cancellationToken);
        records.AddRange(variants.Select(x => new TenantExportRecord(
            "item_variant", x.Id.ToString("D"),
            JsonSerializer.SerializeToElement(new { x.ItemId, x.Sku, x.Name, x.Version }))));

        return new TenantExportSection(ModuleKey, SchemaVersion, records);
    }
}
