using Avila.Modules.Administration.Contracts;
using Avila.Modules.Billing.Contracts;
using Avila.Modules.Inventory.Contracts;
using Avila.Modules.Sales.Contracts;
using Avila.Platform.Tenancy;

namespace Avila.Application;

public sealed record SalesFlowRequest(
    string CompanyName,
    string CompanyTaxId,
    string CustomerName,
    string CustomerTaxId,
    string ItemSku,
    string ItemName,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Quantity,
    string Currency = "VES");

public sealed record SalesFlowResult(
    Guid CompanyId,
    Guid PartyId,
    Guid ItemId,
    Guid QuoteId,
    Guid OrderId,
    Guid InvoiceDraftId,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    string Currency);

/// <summary>
/// First production vertical slice: tenant → company → party → item →
/// quote → order → invoice draft, all in one unit of work.
/// </summary>
public sealed class SalesFlowService
{
    private readonly IAdministrationModule _administration;
    private readonly IInventoryModule _inventory;
    private readonly ISalesModule _sales;
    private readonly IBillingModule _billing;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public SalesFlowService(
        IAdministrationModule administration,
        IInventoryModule inventory,
        ISalesModule sales,
        IBillingModule billing,
        IUnitOfWorkFactory unitOfWorkFactory)
    {
        _administration = administration;
        _inventory = inventory;
        _sales = sales;
        _billing = billing;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<SalesFlowResult> RunAsync(
        string connectionString,
        Guid tenantId,
        SalesFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = await _unitOfWorkFactory.BeginAsync(
            connectionString,
            tenantId,
            cancellationToken);

        var company = await _administration.CreateCompanyAsync(
            unitOfWork,
            new CreateCompanyCommand(request.CompanyName, request.CompanyTaxId),
            cancellationToken);

        var party = await _administration.CreatePartyAsync(
            unitOfWork,
            new CreatePartyCommand(request.CustomerName, request.CustomerTaxId, IsCustomer: true, IsSupplier: false),
            cancellationToken);

        var item = await _inventory.CreateItemAsync(
            unitOfWork,
            new CreateItemCommand(
                request.ItemSku,
                request.ItemName,
                request.UnitPrice,
                request.TaxRate,
                IsService: false),
            cancellationToken);

        var quote = await _sales.CreateQuoteAsync(
            unitOfWork,
            new CreateQuoteCommand(
                company.Id,
                party.Id,
                [new QuoteLineInput(item.Id, item.Name, request.Quantity, item.UnitPrice, item.TaxRate)],
                request.Currency),
            cancellationToken);

        var order = await _sales.CreateOrderFromQuoteAsync(
            unitOfWork,
            new CreateOrderFromQuoteCommand(quote.Id),
            cancellationToken);

        var draft = await _billing.CreateDraftAsync(
            unitOfWork,
            new CreateInvoiceDraftCommand(
                company.Id,
                order.Id,
                party.Id,
                order.Lines
                    .Select(line => new InvoiceDraftLineInput(
                        line.ItemId,
                        line.Description,
                        line.Quantity,
                        line.UnitPrice,
                        line.TaxRate))
                    .ToArray(),
                order.Currency),
            cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        return new SalesFlowResult(
            company.Id,
            party.Id,
            item.Id,
            quote.Id,
            order.Id,
            draft.Id,
            order.Subtotal,
            order.TaxTotal,
            order.Total,
            order.Currency);
    }
}
