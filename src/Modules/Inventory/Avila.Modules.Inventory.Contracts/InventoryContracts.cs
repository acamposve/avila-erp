using Avila.Platform.Tenancy;

namespace Avila.Modules.Inventory.Contracts;

public sealed record CreateItemCommand(
    string Sku,
    string Name,
    decimal UnitPrice,
    decimal TaxRate,
    bool IsService);

public sealed record ItemDto(
    Guid Id,
    Guid TenantId,
    string Sku,
    string Name,
    decimal UnitPrice,
    decimal TaxRate,
    bool IsService);

public interface IInventoryModule
{
    Task<ItemDto> CreateItemAsync(
        IUnitOfWork unitOfWork,
        CreateItemCommand command,
        CancellationToken cancellationToken = default);
}
