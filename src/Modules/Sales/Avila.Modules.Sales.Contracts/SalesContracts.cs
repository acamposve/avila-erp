using Avila.Platform.Tenancy;

namespace Avila.Modules.Sales.Contracts;

public sealed record QuoteLineInput(
    Guid ItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate);

public sealed record CreateQuoteCommand(
    Guid CompanyId,
    Guid PartyId,
    IReadOnlyList<QuoteLineInput> Lines,
    string Currency);

public sealed record QuoteDto(
    Guid Id,
    Guid TenantId,
    Guid CompanyId,
    Guid PartyId,
    string Number,
    string Status,
    decimal Total);

public sealed record CreateOrderFromQuoteCommand(Guid QuoteId);

public sealed record OrderLineDto(
    Guid ItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal LineTotal);

public sealed record OrderDto(
    Guid Id,
    Guid TenantId,
    Guid CompanyId,
    Guid PartyId,
    Guid QuoteId,
    string Number,
    string Status,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    string Currency,
    IReadOnlyList<OrderLineDto> Lines);

public interface ISalesModule
{
    Task<QuoteDto> CreateQuoteAsync(
        IUnitOfWork unitOfWork,
        CreateQuoteCommand command,
        CancellationToken cancellationToken = default);

    Task<OrderDto> CreateOrderFromQuoteAsync(
        IUnitOfWork unitOfWork,
        CreateOrderFromQuoteCommand command,
        CancellationToken cancellationToken = default);
}
