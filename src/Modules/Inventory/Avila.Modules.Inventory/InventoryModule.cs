using Avila.Modules.Inventory.Contracts;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Avila.Modules.Inventory;

public sealed class InventoryModule : IInventoryModule
{
    public async Task<ItemDto> CreateItemAsync(
        IUnitOfWork unitOfWork,
        CreateItemCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Sku) || string.IsNullOrWhiteSpace(command.Name))
        {
            throw new DomainException("Item SKU and name are required.");
        }

        if (command.UnitPrice < 0 || command.TaxRate < 0)
        {
            throw new DomainException("Item price and tax rate cannot be negative.");
        }

        await using var context = new InventoryDbContext(
            unitOfWork.OptionsFor<InventoryDbContext>("inventory"));
        unitOfWork.Enlist(context);

        var item = new Item
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            Sku = command.Sku.Trim(),
            Name = command.Name.Trim(),
            UnitPrice = decimal.Round(command.UnitPrice, 6, MidpointRounding.AwayFromZero),
            TaxRate = command.TaxRate,
            IsService = command.IsService,
            TaxClassificationCode = command.TaxClassificationCode?.Trim(),
            CategoryId = command.CategoryId,
            UnitOfMeasureId = command.UnitOfMeasureId
        };

        context.Items.Add(item);
        await context.SaveChangesAsync(cancellationToken);

        return new ItemDto(
            item.Id,
            item.TenantId,
            item.Sku,
            item.Name,
            item.UnitPrice,
            item.TaxRate,
            item.IsService);
    }

    public async Task<CategoryDto> CreateCategoryAsync(
        IUnitOfWork unitOfWork,
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new DomainException("Category name is required.");
        }

        await using var context = Context(unitOfWork);
        var category = new Category
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            Name = command.Name.Trim(),
            ParentCode = command.ParentCode?.Trim()
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);
        return new CategoryDto(category.Id, category.Name, category.ParentCode);
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(
        IUnitOfWork unitOfWork,
        CreateWarehouseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.Name))
        {
            throw new DomainException("Warehouse code and name are required.");
        }

        await using var context = Context(unitOfWork);
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            Code = command.Code.Trim(),
            Name = command.Name.Trim(),
            Address = command.Address?.Trim()
        };
        context.Warehouses.Add(warehouse);
        await context.SaveChangesAsync(cancellationToken);
        return new WarehouseDto(
            warehouse.Id,
            warehouse.Code,
            warehouse.Name,
            warehouse.Address,
            warehouse.IsActive);
    }

    public async Task<UnitOfMeasureDto> CreateUnitOfMeasureAsync(
        IUnitOfWork unitOfWork,
        CreateUnitOfMeasureCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.Name))
        {
            throw new DomainException("Unit of measure code and name are required.");
        }

        await using var context = Context(unitOfWork);
        var uom = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            Code = command.Code.Trim(),
            Name = command.Name.Trim()
        };
        context.UnitOfMeasures.Add(uom);
        await context.SaveChangesAsync(cancellationToken);
        return new UnitOfMeasureDto(uom.Id, uom.Code, uom.Name);
    }

    public async Task<ItemVariantDto> CreateVariantAsync(
        IUnitOfWork unitOfWork,
        CreateItemVariantCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Sku) || string.IsNullOrWhiteSpace(command.Name))
        {
            throw new DomainException("Variant SKU and name are required.");
        }

        await using var context = Context(unitOfWork);
        var itemExists = await context.Items.AnyAsync(
            x => x.Id == command.ItemId && x.TenantId == unitOfWork.TenantId,
            cancellationToken);
        if (!itemExists)
        {
            throw new DomainException($"Item {command.ItemId} was not found.");
        }

        var variant = new ItemVariant
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            ItemId = command.ItemId,
            Sku = command.Sku.Trim(),
            Name = command.Name.Trim()
        };
        context.ItemVariants.Add(variant);
        await context.SaveChangesAsync(cancellationToken);
        return new ItemVariantDto(variant.Id, variant.ItemId, variant.Sku, variant.Name);
    }

    public async Task<BinLocationDto> CreateBinLocationAsync(
        IUnitOfWork unitOfWork,
        CreateBinLocationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.Name))
        {
            throw new DomainException("Bin code and name are required.");
        }

        await using var context = Context(unitOfWork);
        var warehouseExists = await context.Warehouses.AnyAsync(
            x => x.Id == command.WarehouseId && x.TenantId == unitOfWork.TenantId,
            cancellationToken);
        if (!warehouseExists)
        {
            throw new DomainException($"Warehouse {command.WarehouseId} was not found.");
        }

        var bin = new BinLocation
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            WarehouseId = command.WarehouseId,
            Code = command.Code.Trim(),
            Name = command.Name.Trim()
        };
        context.BinLocations.Add(bin);
        await context.SaveChangesAsync(cancellationToken);
        return new BinLocationDto(bin.Id, bin.WarehouseId, bin.Code, bin.Name);
    }

    private static InventoryDbContext Context(IUnitOfWork unitOfWork)
    {
        var context = new InventoryDbContext(
            unitOfWork.OptionsFor<InventoryDbContext>("inventory"));
        unitOfWork.Enlist(context);
        return context;
    }
}
