using Avila.Spike.Accounting;
using Avila.Spike.Billing;
using Avila.Spike.Tenancy;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class AccountingPostingTests
{
    private readonly PostgresFixture _fixture;

    public AccountingPostingTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T05_invoice_and_journal_are_one_unit_of_work()
    {
        var tenant = Guid.NewGuid();
        var company = Guid.NewGuid();
        var debitAccount = await TestDb.SeedAccountAsync(
            _fixture.AdminConnectionString,
            tenant,
            "1100",
            "Accounts receivable");
        var creditAccount = await TestDb.SeedAccountAsync(
            _fixture.AdminConnectionString,
            tenant,
            "4100",
            "Revenue");

        var invoiceId = await SalesInvoiceService.IssueAsync(
            _fixture.AppConnectionString,
            tenant,
            company,
            "company",
            company,
            "INV",
            2026,
            amount: 100m,
            debitAccountId: debitAccount,
            creditAccountId: creditAccount);

        Assert.Equal(1, await TestDb.CountInvoicesAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(1, await TestDb.CountJournalEntriesAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(2, await TestDb.CountJournalLinesAsync(_fixture.AdminConnectionString, tenant));
        Assert.NotNull(await TestDb.GetInvoiceJournalEntryIdAsync(
            _fixture.AdminConnectionString,
            tenant,
            invoiceId));

        var (debit, credit) = await TestDb.GetJournalTotalsAsync(
            _fixture.AdminConnectionString,
            tenant);
        Assert.Equal(100m, debit);
        Assert.Equal(100m, credit);
    }

    [Fact]
    public async Task T15_duplicate_accounting_event_is_rejected_without_a_second_entry()
    {
        var tenant = Guid.NewGuid();
        var company = Guid.NewGuid();
        var debitAccount = await TestDb.SeedAccountAsync(
            _fixture.AdminConnectionString,
            tenant,
            "1100");
        var creditAccount = await TestDb.SeedAccountAsync(
            _fixture.AdminConnectionString,
            tenant,
            "4100");

        var invoiceId = await SalesInvoiceService.IssueAsync(
            _fixture.AppConnectionString,
            tenant,
            company,
            "company",
            company,
            "INV",
            2026,
            amount: 100m,
            debitAccountId: debitAccount,
            creditAccountId: creditAccount);

        var exception = await Assert.ThrowsAsync<AccountingDuplicatePostingException>(async () =>
        {
            await using var unitOfWork = await TenantUnitOfWork.BeginAsync(
                _fixture.AppConnectionString,
                tenant);

            await AccountingService.PostInvoiceAsync(
                unitOfWork.Connection,
                unitOfWork.Transaction,
                tenant,
                invoiceId,
                amount: 100m,
                debitAccountId: debitAccount,
                creditAccountId: creditAccount);
        });

        Assert.Contains("already exists", exception.Message);
        Assert.Equal(1, await TestDb.CountJournalEntriesAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(2, await TestDb.CountJournalLinesAsync(_fixture.AdminConnectionString, tenant));
    }

    [Fact]
    public async Task T06_accounting_failure_rolls_back_invoice_receivable_and_number()
    {
        var tenant = Guid.NewGuid();
        var company = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<AccountingPostingException>(() =>
            SalesInvoiceService.IssueAsync(
                _fixture.AppConnectionString,
                tenant,
                company,
                "company",
                company,
                "INV",
                2026,
                amount: 100m,
                debitAccountId: Guid.NewGuid(),
                creditAccountId: Guid.NewGuid()));

        Assert.Contains("missing or not postable", exception.Message);
        Assert.Equal(0, await TestDb.CountInvoicesAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(0, await TestDb.CountReceivablesAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(0, await TestDb.CountJournalEntriesAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(0, await TestDb.CountSequencesAsync(_fixture.AdminConnectionString, tenant));
    }
}
