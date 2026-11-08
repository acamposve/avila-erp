using Avila.Spike.Accounting;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class PeriodCloseTests
{
    private readonly PostgresFixture _fixture;

    public PeriodCloseTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T19_close_blocks_and_rejects_a_concurrent_posting()
    {
        var tenant = Guid.NewGuid();
        var company = Guid.NewGuid();
        var period = await TestDb.SeedPeriodAsync(
            _fixture.AdminConnectionString,
            tenant,
            company);

        var closingStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseClose = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var close = PeriodService.CloseAsync(
            _fixture.AppConnectionString,
            tenant,
            company,
            period,
            async () =>
            {
                closingStarted.TrySetResult(true);
                await releaseClose.Task;
            });

        await closingStarted.Task.WaitAsync(TimeSpan.FromSeconds(15));

        var posting = RunAsync(() => PeriodService.PostJournalEntryAsync(
            _fixture.AppConnectionString,
            tenant,
            company,
            period,
            Guid.NewGuid(),
            amount: 100m,
            debitAccountId: Guid.NewGuid(),
            creditAccountId: Guid.NewGuid()));

        await Task.Delay(150);
        releaseClose.TrySetResult(true);

        await close.WaitAsync(TimeSpan.FromSeconds(30));
        var postingException = await posting.WaitAsync(TimeSpan.FromSeconds(30));

        Assert.IsType<PeriodClosedException>(postingException);
        Assert.Equal("closed", await TestDb.GetPeriodStatusAsync(
            _fixture.AdminConnectionString,
            tenant,
            company,
            period));
        Assert.Equal(0, await TestDb.CountJournalEntriesAsync(_fixture.AdminConnectionString, tenant));
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
