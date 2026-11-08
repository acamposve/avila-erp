using Avila.Spike.Inventory;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class TransferConcurrencyTests
{
    private readonly PostgresFixture _fixture;

    public TransferConcurrencyTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T02_crossing_transfers_use_the_same_lock_order_and_preserve_totals()
    {
        var tenant = Guid.NewGuid();
        var item = Guid.NewGuid();
        var warehouseA = Guid.NewGuid();
        var warehouseB = Guid.NewGuid();

        await TestDb.SeedStockAsync(
            _fixture.AdminConnectionString,
            tenant,
            warehouseA,
            item,
            quantity: 100m);
        await TestDb.SeedStockAsync(
            _fixture.AdminConnectionString,
            tenant,
            warehouseB,
            item,
            quantity: 100m);

        var transferAToB = TransferService.TransferAsync(
            _fixture.AppConnectionString,
            tenant,
            warehouseA,
            warehouseB,
            item,
            quantity: 10m,
            transferId: Guid.NewGuid());

        var transferBToA = TransferService.TransferAsync(
            _fixture.AppConnectionString,
            tenant,
            warehouseB,
            warehouseA,
            item,
            quantity: 10m,
            transferId: Guid.NewGuid());

        await Task.WhenAll(transferAToB, transferBToA).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(100m, await TestDb.GetStockAsync(
            _fixture.AdminConnectionString,
            tenant,
            warehouseA,
            item));
        Assert.Equal(100m, await TestDb.GetStockAsync(
            _fixture.AdminConnectionString,
            tenant,
            warehouseB,
            item));
        Assert.Equal(4, await TestDb.CountMovementsAsync(
            _fixture.AdminConnectionString,
            tenant,
            item));
    }
}
