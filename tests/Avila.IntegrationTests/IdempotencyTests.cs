using Avila.Spike.Platform;
using Avila.Spike.Tenancy;
using Npgsql;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class IdempotencyTests
{
    private readonly PostgresFixture _fixture;

    public IdempotencyTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T18_concurrent_same_key_executes_once_and_replays_response()
    {
        var tenant = Guid.NewGuid();
        var firstStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = IdempotencyService.ExecuteAsync(
            _fixture.AppConnectionString,
            tenant,
            "pay-1",
            "payment",
            "hash-1",
            async (connection, transaction) =>
            {
                await InsertNoteAsync(connection, transaction, tenant, "first");
                return "ok";
            },
            async () =>
            {
                firstStarted.TrySetResult(true);
                await releaseFirst.Task;
            });

        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(15));

        var second = IdempotencyService.ExecuteAsync(
            _fixture.AppConnectionString,
            tenant,
            "pay-1",
            "payment",
            "hash-1",
            async (connection, transaction) =>
            {
                await InsertNoteAsync(connection, transaction, tenant, "second");
                return "ok";
            });

        await Task.Delay(150);
        releaseFirst.TrySetResult(true);

        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(2, results.Length);
        Assert.All(results, result => Assert.Equal("ok", result.Response));
        Assert.Equal(1, results.Count(result => result.Replayed));
        Assert.Equal(1, await TestDb.CountTenantNotesAsync(_fixture.AdminConnectionString, tenant));
    }

    private static async Task InsertNoteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        string note)
    {
        await using var command = new NpgsqlCommand(
            "INSERT INTO platform.tenant_notes (id, tenant_id, note) VALUES (@id, @tenant, @note)",
            connection,
            transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("note", note);
        await command.ExecuteNonQueryAsync();
    }
}
