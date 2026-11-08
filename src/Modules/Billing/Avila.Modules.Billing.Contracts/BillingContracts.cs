using Avila.Platform.Tenancy;

namespace Avila.Modules.Billing.Contracts;

public sealed record InvoiceDraftLineInput(
    Guid ItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate);

public sealed record CreateInvoiceDraftCommand(
    Guid CompanyId,
    Guid OrderId,
    Guid PartyId,
    IReadOnlyList<InvoiceDraftLineInput> Lines,
    string Currency);

public sealed record InvoiceDraftDto(
    Guid Id,
    Guid TenantId,
    Guid CompanyId,
    Guid OrderId,
    string Status,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    string Currency);

public interface IBillingModule
{
    Task<InvoiceDraftDto> CreateDraftAsync(
        IUnitOfWork unitOfWork,
        CreateInvoiceDraftCommand command,
        CancellationToken cancellationToken = default);
}
