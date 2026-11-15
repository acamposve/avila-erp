using Avila.Modules.Inventory;
using Avila.Modules.Inventory.Contracts;
using Avila.Platform.Authorization;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;

namespace Avila.Api.Inventory;

public sealed record CreateCategoryRequest(string Name, string? ParentCode);
public sealed record CreateWarehouseRequest(string Code, string Name, string? Address);
public sealed record CreateUomRequest(string Code, string Name);
public sealed record CreateItemRequest(
    string Sku,
    string Name,
    decimal UnitPrice,
    bool IsService,
    string TaxClassificationCode,
    Guid? CategoryId,
    Guid? UnitOfMeasureId);
public sealed record CreateVariantRequest(Guid ItemId, string Sku, string Name);
public sealed record CreateBinRequest(Guid WarehouseId, string Code, string Name);

public static class InventoryCatalogEndpoints
{
    public static IEndpointRouteBuilder MapInventoryCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/inventory/categories", CreateCategoryAsync);
        app.MapPost("/api/v1/inventory/warehouses", CreateWarehouseAsync);
        app.MapPost("/api/v1/inventory/uoms", CreateUomAsync);
        app.MapPost("/api/v1/inventory/items", CreateItemAsync);
        app.MapPost("/api/v1/inventory/variants", CreateVariantAsync);
        app.MapPost("/api/v1/inventory/bins", CreateBinAsync);
        return app;
    }

    private static async Task<IResult> CreateCategoryAsync(
        CreateCategoryRequest request,
        HttpContext context,
        IInventoryModule module,
        AuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(context, authorization, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var dto = await module.CreateCategoryAsync(
            Unit(context), new CreateCategoryCommand(request.Name, request.ParentCode), cancellationToken);
        await AuditAsync(context, "inventory.category.created", "category", dto.Id, dto, cancellationToken);
        return Results.Created($"/api/v1/inventory/categories/{dto.Id}", dto);
    }

    private static async Task<IResult> CreateWarehouseAsync(
        CreateWarehouseRequest request,
        HttpContext context,
        IInventoryModule module,
        AuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(context, authorization, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var dto = await module.CreateWarehouseAsync(
            Unit(context), new CreateWarehouseCommand(request.Code, request.Name, request.Address), cancellationToken);
        await AuditAsync(context, "inventory.warehouse.created", "warehouse", dto.Id, dto, cancellationToken);
        return Results.Created($"/api/v1/inventory/warehouses/{dto.Id}", dto);
    }

    private static async Task<IResult> CreateUomAsync(
        CreateUomRequest request,
        HttpContext context,
        IInventoryModule module,
        AuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(context, authorization, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var dto = await module.CreateUnitOfMeasureAsync(
            Unit(context), new CreateUnitOfMeasureCommand(request.Code, request.Name), cancellationToken);
        await AuditAsync(context, "inventory.uom.created", "unit_of_measure", dto.Id, dto, cancellationToken);
        return Results.Created($"/api/v1/inventory/uoms/{dto.Id}", dto);
    }

    private static async Task<IResult> CreateItemAsync(
        CreateItemRequest request,
        HttpContext context,
        IInventoryModule module,
        AuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(context, authorization, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.TaxClassificationCode))
        {
            return Results.BadRequest(new { error = "TAX_CLASSIFICATION_REQUIRED" });
        }

        var dto = await module.CreateItemAsync(
            Unit(context),
            new CreateItemCommand(
                request.Sku,
                request.Name,
                request.UnitPrice,
                0m,
                request.IsService,
                request.TaxClassificationCode,
                request.CategoryId,
                request.UnitOfMeasureId),
            cancellationToken);
        await AuditAsync(context, "inventory.item.created", "item", dto.Id, dto, cancellationToken);
        return Results.Created($"/api/v1/inventory/items/{dto.Id}", dto);
    }

    private static async Task<IResult> CreateVariantAsync(
        CreateVariantRequest request,
        HttpContext context,
        IInventoryModule module,
        AuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(context, authorization, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var dto = await module.CreateVariantAsync(
            Unit(context), new CreateItemVariantCommand(request.ItemId, request.Sku, request.Name), cancellationToken);
        await AuditAsync(context, "inventory.variant.created", "item_variant", dto.Id, dto, cancellationToken);
        return Results.Created($"/api/v1/inventory/variants/{dto.Id}", dto);
    }

    private static async Task<IResult> CreateBinAsync(
        CreateBinRequest request,
        HttpContext context,
        IInventoryModule module,
        AuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(context, authorization, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var dto = await module.CreateBinLocationAsync(
            Unit(context), new CreateBinLocationCommand(request.WarehouseId, request.Code, request.Name), cancellationToken);
        await AuditAsync(context, "inventory.bin.created", "bin_location", dto.Id, dto, cancellationToken);
        return Results.Created($"/api/v1/inventory/bins/{dto.Id}", dto);
    }

    private static async Task<bool> AuthorizeAsync(
        HttpContext context,
        AuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        if (context.Items[typeof(SessionPrincipal)] is not SessionPrincipal principal
            || context.Items[typeof(IUnitOfWork)] is not IUnitOfWork unitOfWork)
        {
            return false;
        }

        try
        {
            await authorization.EnsureAllowedAsync(
                unitOfWork,
                principal,
                Permissions.InventoryStockAdjust,
                correlationId: context.TraceIdentifier,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (AuthorizationException)
        {
            return false;
        }
    }

    private static async Task AuditAsync<T>(
        HttpContext context,
        string action,
        string entityType,
        Guid entityId,
        T dto,
        CancellationToken cancellationToken)
    {
        await new AuditService().RecordAsync(
            Unit(context),
            (SessionPrincipal)context.Items[typeof(SessionPrincipal)]!,
            action,
            entityType,
            entityId,
            before: null,
            after: dto,
            context.TraceIdentifier,
            cancellationToken);
    }

    private static IUnitOfWork Unit(HttpContext context)
        => (IUnitOfWork)context.Items[typeof(IUnitOfWork)]!;
}
