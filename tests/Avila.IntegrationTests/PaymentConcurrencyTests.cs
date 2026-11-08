using Avila.Spike.Treasury;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class PaymentConcurrencyTests
{
    private readonly PostgresFixture _fixture;

    public PaymentConcurrencyTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T03_two_concurrent_payments_cannot_over_allocate()
    {
        var tenant = Guid.NewGuid();
        var receivable = Guid.NewGuid();
        var paymentA = Guid.NewGuid();
        var paymentB = Guid.NewGuid();

        await TestDb.SeedReceivableAsync(
            _fixture.AdminConnectionString,
            tenant,
            receivable,
            originalAmount: 100m);

        var firstLocked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var taskA = RunAsync(() => PaymentService.ApplyAsync(
            _fixture.AppConnectionString,
            tenant,
            receivable,
            paymentA,
            amount: 60m,
            async () =>
            {
                firstLocked.TrySetResult(true);
                await releaseFirst.Task;
            }));

        await firstLocked.Task.WaitAsync(TimeSpan.FromSeconds(15));

        var taskB = RunAsync(() => PaymentService.ApplyAsync(
            _fixture.AppConnectionString,
            tenant,
            receivable,
            paymentB,
            amount: 60m));

        await Task.Delay(150);
        releaseFirst.TrySetResult(true);

        var results = await Task.WhenAll(taskA, taskB).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(1, results.Count(result => result is null));
        Assert.Equal(1, results.Count(result => result is PaymentOverAllocatedException));
        Assert.Equal(60m, await TestDb.GetAppliedAmountAsync(
            _fixture.AdminConnectionString,
            tenant,
            receivable));
        Assert.Equal(1, await TestDb.CountAllocationsAsync(
            _fixture.AdminConnectionString,
            tenant,
            receivable));
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
