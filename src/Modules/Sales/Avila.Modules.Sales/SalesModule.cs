using Avila.Modules.Sales.Contracts;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Avila.Modules.Sales;

public sealed class SalesModule : ISalesModule
{
    public async Task<QuoteDto> CreateQuoteAsync(
        IUnitOfWork unitOfWork,
        CreateQuoteCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count == 0)
        {
            throw new DomainException("A quote requires at least one line.");
        }

        await using var context = new SalesDbContext(
            unitOfWork.OptionsFor<SalesDbContext>("sales"));
        unitOfWork.Enlist(context);

        var quoteId = Guid.NewGuid();
        var quote = new Quote
        {
            Id = quoteId,
            TenantId = unitOfWork.TenantId,
            CompanyId = command.CompanyId,
            PartyId = command.PartyId,
            Number = $"Q-{quoteId:N}",
            Status = "draft",
            Currency = command.Currency
        };

        foreach (var input in command.Lines)
        {
            var lineTotal = Round(input.Quantity * input.UnitPrice);
            quote.Lines.Add(new QuoteLine
            {
                Id = Guid.NewGuid(),
                TenantId = unitOfWork.TenantId,
                QuoteId = quoteId,
                ItemId = input.ItemId,
                Description = input.Description,
                Quantity = input.Quantity,
                UnitPrice = input.UnitPrice,
                TaxRate = input.TaxRate,
                LineTotal = lineTotal
            });
        }

        quote.Subtotal = quote.Lines.Sum(x => x.LineTotal);
        quote.TaxTotal = quote.Lines.Sum(x => Round(x.LineTotal * x.TaxRate));
        quote.Total = quote.Subtotal + quote.TaxTotal;

        context.Quotes.Add(quote);
        await context.SaveChangesAsync(cancellationToken);

        return new QuoteDto(
            quote.Id,
            quote.TenantId,
            quote.CompanyId,
            quote.PartyId,
            quote.Number,
            quote.Status,
            quote.Total);
    }

    public async Task<OrderDto> CreateOrderFromQuoteAsync(
        IUnitOfWork unitOfWork,
        CreateOrderFromQuoteCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var context = new SalesDbContext(
            unitOfWork.OptionsFor<SalesDbContext>("sales"));
        unitOfWork.Enlist(context);

        var quote = await context.Quotes
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(
                x => x.Id == command.QuoteId && x.TenantId == unitOfWork.TenantId,
                cancellationToken)
            ?? throw new DomainException($"Quote {command.QuoteId} was not found.");

        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            TenantId = unitOfWork.TenantId,
            CompanyId = quote.CompanyId,
            PartyId = quote.PartyId,
            QuoteId = quote.Id,
            Number = $"O-{orderId:N}",
            Status = "confirmed",
            Subtotal = quote.Subtotal,
            TaxTotal = quote.TaxTotal,
            Total = quote.Total,
            Currency = quote.Currency
        };

        foreach (var line in quote.Lines)
        {
            order.Lines.Add(new OrderLine
            {
                Id = Guid.NewGuid(),
                TenantId = unitOfWork.TenantId,
                OrderId = orderId,
                ItemId = line.ItemId,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                TaxRate = line.TaxRate,
                LineTotal = line.LineTotal
            });
        }

        context.Orders.Add(order);
        quote.Status = "ordered";
        await context.SaveChangesAsync(cancellationToken);

        return new OrderDto(
            order.Id,
            order.TenantId,
            order.CompanyId,
            order.PartyId,
            order.QuoteId,
            order.Number,
            order.Status,
            order.Subtotal,
            order.TaxTotal,
            order.Total,
            order.Currency,
            order.Lines.Select(line => new OrderLineDto(
                line.ItemId,
                line.Description,
                line.Quantity,
                line.UnitPrice,
                line.TaxRate,
                line.LineTotal)).ToArray());
    }

    private static decimal Round(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
