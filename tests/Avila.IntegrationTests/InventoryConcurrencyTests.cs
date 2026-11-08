using Avila.Spike.Inventory;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class InventoryConcurrencyTests
{
    private readonly PostgresFixture _fixture;

    public InventoryConcurrencyTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T01_two_concurrent_issues_cannot_oversell()
    {
        var tenant = Guid.NewGuid();
        var warehouse = Guid.NewGuid();
        var item = Guid.NewGuid();

        await TestDb.SeedStockAsync(
            _fixture.AdminConnectionString,
            tenant,
            warehouse,
            item,
            quantity: 10m);

        var firstLocked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var requestA = new StockIssueRequest(tenant, warehouse, item, 8m, Guid.NewGuid(), Guid.NewGuid());
        var requestB = new StockIssueRequest(tenant, warehouse, item, 8m, Guid.NewGuid(), Guid.NewGuid());

        var taskA = RunAsync(() => InventoryService.IssueAsync(
            _fixture.AppConnectionString,
            requestA,
            async () =>
            {
                firstLocked.TrySetResult(true);
                await releaseFirst.Task;
            }));

        await firstLocked.Task.WaitAsync(TimeSpan.FromSeconds(15));

        var taskB = RunAsync(() => InventoryService.IssueAsync(
            _fixture.AppConnectionString,
            requestB));

        // Give B time to attempt the lock while A holds it.
        await Task.Delay(150);
        releaseFirst.TrySetResult(true);

        var results = await Task.WhenAll(taskA, taskB).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(1, results.Count(result => result is null));
        Assert.Equal(1, results.Count(result => result is StockInsufficientException));
        Assert.Equal(2m, await TestDb.GetStockAsync(
            _fixture.AdminConnectionString,
            tenant,
            warehouse,
            item));
        Assert.Equal(1, await TestDb.CountMovementsAsync(
            _fixture.AdminConnectionString,
            tenant,
            item));
    }

    private static async Task<Exception?> RunAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
