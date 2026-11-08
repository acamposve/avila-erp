using Avila.Spike.Accounting;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class CutoverTests
{
    private readonly PostgresFixture _fixture;

    public CutoverTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T12_cutover_opens_balances_links_sources_and_is_idempotent()
    {
        var tenant = Guid.NewGuid();
        var company = Guid.NewGuid();
        var warehouse = Guid.NewGuid();
        var item = Guid.NewGuid();
        var invoiceId = await TestDb.SeedInvoiceAsync(
            _fixture.AdminConnectionString,
            tenant,
            number: 1);
        var receivableId = Guid.NewGuid();
        await TestDb.SeedReceivableAsync(
            _fixture.AdminConnectionString,
            tenant,
            receivableId,
            originalAmount: 100m,
            invoiceId: invoiceId);
        await TestDb.SeedStockAsync(
            _fixture.AdminConnectionString,
            tenant,
            warehouse,
            item,
            quantity: 10m,
            averageCost: 10m);

        var receivableAccount = await TestDb.SeedAccountAsync(
            _fixture.AdminConnectionString,
            tenant,
            "1100",
            "Accounts receivable");
        var inventoryAccount = await TestDb.SeedAccountAsync(
            _fixture.AdminConnectionString,
            tenant,
            "1200",
            "Inventory");
        var equityAccount = await TestDb.SeedAccountAsync(
            _fixture.AdminConnectionString,
            tenant,
            "3000",
            "Opening equity");

        var cutoverId = await CutoverService.RunAsync(
            _fixture.AppConnectionString,
            tenant,
            company,
            new DateOnly(2026, 1, 1),
            receivableAccount,
            inventoryAccount,
            equityAccount);

        Assert.Equal("active", await TestDb.GetCutoverStatusAsync(
            _fixture.AdminConnectionString,
            tenant,
            cutoverId));
        Assert.True(await TestDb.IsAccountingEnabledAsync(
            _fixture.AdminConnectionString,
            tenant,
            company));
        Assert.Equal(1, await TestDb.CountCutoverRunsAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(2, await TestDb.CountJournalEntriesAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(4, await TestDb.CountJournalLinesAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(1, await TestDb.CountOpeningLinksAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal("opening_linked", await TestDb.GetInvoiceAccountingStatusAsync(
            _fixture.AdminConnectionString,
            tenant,
            invoiceId));

        var (debit, credit) = await TestDb.GetJournalTotalsAsync(
            _fixture.AdminConnectionString,
            tenant);
        Assert.Equal(200m, debit);
        Assert.Equal(200m, credit);

        var secondRun = await CutoverService.RunAsync(
            _fixture.AppConnectionString,
            tenant,
            company,
            new DateOnly(2026, 1, 1),
            receivableAccount,
            inventoryAccount,
            equityAccount);

        Assert.Equal(cutoverId, secondRun);
        Assert.Equal(1, await TestDb.CountCutoverRunsAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(2, await TestDb.CountJournalEntriesAsync(_fixture.AdminConnectionString, tenant));
    }
}
