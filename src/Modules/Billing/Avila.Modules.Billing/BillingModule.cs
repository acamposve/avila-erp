using Avila.Modules.Billing.Contracts;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;

namespace Avila.Modules.Billing;

public sealed class BillingModule : IBillingModule
{
    public async Task<InvoiceDraftDto> CreateDraftAsync(
        IUnitOfWork unitOfWork,
        CreateInvoiceDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count == 0)
        {
            throw new DomainException("An invoice draft requires at least one line.");
        }

        await using var context = new BillingDbContext(
            unitOfWork.OptionsFor<BillingDbContext>("billing"));
        unitOfWork.Enlist(context);

        var draftId = Guid.NewGuid();
        var draft = new InvoiceDraft
        {
            Id = draftId,
            TenantId = unitOfWork.TenantId,
            CompanyId = command.CompanyId,
            OrderId = command.OrderId,
            PartyId = command.PartyId,
            Status = "draft",
            Currency = command.Currency
        };

        foreach (var input in command.Lines)
        {
            var lineTotal = Round(input.Quantity * input.UnitPrice);
            draft.Lines.Add(new InvoiceDraftLine
            {
                Id = Guid.NewGuid(),
                TenantId = unitOfWork.TenantId,
                InvoiceDraftId = draftId,
                ItemId = input.ItemId,
                Description = input.Description,
                Quantity = input.Quantity,
                UnitPrice = input.UnitPrice,
                TaxRate = input.TaxRate,
                LineTotal = lineTotal
            });
        }

        draft.Subtotal = draft.Lines.Sum(x => x.LineTotal);
        draft.TaxTotal = draft.Lines.Sum(x => Round(x.LineTotal * x.TaxRate));
        draft.Total = draft.Subtotal + draft.TaxTotal;

        context.InvoiceDrafts.Add(draft);
        await context.SaveChangesAsync(cancellationToken);

        return new InvoiceDraftDto(
            draft.Id,
            draft.TenantId,
            draft.CompanyId,
            draft.OrderId,
            draft.Status,
            draft.Subtotal,
            draft.TaxTotal,
            draft.Total,
            draft.Currency);
    }

    private static decimal Round(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
