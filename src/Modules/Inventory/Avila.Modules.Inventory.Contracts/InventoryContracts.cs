using Avila.Platform.Tenancy;

namespace Avila.Modules.Inventory.Contracts;

public sealed record CreateItemCommand(
    string Sku,
    string Name,
    decimal UnitPrice,
    decimal TaxRate,
    bool IsService,
    string? TaxClassificationCode = null,
    Guid? CategoryId = null,
    Guid? UnitOfMeasureId = null);

public sealed record ItemDto(
    Guid Id,
    Guid TenantId,
    string Sku,
    string Name,
    decimal UnitPrice,
    decimal TaxRate,
    bool IsService,
    string? TaxClassificationCode = null,
    Guid? CategoryId = null,
    Guid? UnitOfMeasureId = null,
    int Version = 1);

public sealed record CreateCategoryCommand(string Name, string? ParentCode = null);
public sealed record CategoryDto(Guid Id, string Name, string? ParentCode, int Version = 1);

public sealed record CreateWarehouseCommand(string Code, string Name, string? Address = null);
public sealed record WarehouseDto(Guid Id, string Code, string Name, string? Address, bool IsActive, int Version = 1);

public sealed record CreateUnitOfMeasureCommand(string Code, string Name);
public sealed record UnitOfMeasureDto(Guid Id, string Code, string Name, int Version = 1);

public sealed record CreateItemVariantCommand(Guid ItemId, string Sku, string Name);
public sealed record ItemVariantDto(Guid Id, Guid ItemId, string Sku, string Name, int Version = 1);

public sealed record CreateBinLocationCommand(Guid WarehouseId, string Code, string Name);
public sealed record BinLocationDto(Guid Id, Guid WarehouseId, string Code, string Name, int Version = 1);

public interface IInventoryModule
{
    Task<ItemDto> CreateItemAsync(
        IUnitOfWork unitOfWork,
        CreateItemCommand command,
        CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateCategoryAsync(
        IUnitOfWork unitOfWork,
        CreateCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<WarehouseDto> CreateWarehouseAsync(
        IUnitOfWork unitOfWork,
        CreateWarehouseCommand command,
        CancellationToken cancellationToken = default);

    Task<UnitOfMeasureDto> CreateUnitOfMeasureAsync(
        IUnitOfWork unitOfWork,
        CreateUnitOfMeasureCommand command,
        CancellationToken cancellationToken = default);

    Task<ItemVariantDto> CreateVariantAsync(
        IUnitOfWork unitOfWork,
        CreateItemVariantCommand command,
        CancellationToken cancellationToken = default);

    Task<BinLocationDto> CreateBinLocationAsync(
        IUnitOfWork unitOfWork,
        CreateBinLocationCommand command,
        CancellationToken cancellationToken = default);
}
