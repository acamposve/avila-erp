using Avila.Spike.Accounting;
using Avila.Spike.Billing;
using Avila.Spike.Tenancy;
using Avila.Spike.Treasury;

namespace Avila.Spike.Billing;

/// <summary>
/// Spike-only invoice issuance path. It proves that invoice, receivable and journal
/// entry are one unit of work when accounting is active.
/// </summary>
public static class SalesInvoiceService
{
    public static async Task<Guid> IssueAsync(
        string connectionString,
        Guid tenantId,
        Guid companyId,
        string scopeType,
        Guid scopeId,
        string series,
        int fiscalYear,
        decimal amount,
        Guid debitAccountId,
        Guid creditAccountId,
        CancellationToken cancellationToken = default)
    {
        var invoiceId = Guid.NewGuid();

        await using var unitOfWork = await TenantUnitOfWork.BeginAsync(
            connectionString,
            tenantId,
            cancellationToken);

        var number = await DocumentNumberService.NextAsync(
            unitOfWork.Connection,
            unitOfWork.Transaction,
            tenantId,
            companyId,
            scopeType,
            scopeId,
            "invoice",
            series,
            fiscalYear,
            cancellationToken);

        unitOfWork.Billing.Invoices.Add(new Invoice
        {
            Id = invoiceId,
            TenantId = tenantId,
            CompanyId = companyId,
            Number = number,
            Status = "issued",
            AccountingStatus = "posted"
        });

        await unitOfWork.SaveBillingAsync(cancellationToken);

        unitOfWork.Treasury.Receivables.Add(new Receivable
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CustomerId = Guid.NewGuid(),
            OriginalAmount = amount,
            AppliedAmount = 0m,
            Currency = "VES",
            Status = "open"
        });

        await unitOfWork.SaveTreasuryAsync(cancellationToken);

        await AccountingService.PostInvoiceAsync(
            unitOfWork.Connection,
            unitOfWork.Transaction,
            tenantId,
            invoiceId,
            amount,
            debitAccountId,
            creditAccountId,
            cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);
        return invoiceId;
    }
}
