using Avila.Modules.Inventory.Contracts;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;

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
            UnitPrice = decimal.Round(command.UnitPrice, 4, MidpointRounding.AwayFromZero),
            TaxRate = command.TaxRate,
            IsService = command.IsService
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
}
