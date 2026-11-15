using Avila.Platform.Outbox;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;
using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class R2FoundationTests
{
    private readonly ProductionFixture _fixture;

    public R2FoundationTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Money_rounding_and_currency_pair_are_deterministic()
    {
        Assert.Equal(1.01m, Money.Round(1.005m, 2));
        Assert.Equal(2.01m, Money.Round(2.005m, 2));
        var pair = new CurrencyPair("USD", "VES", 36.5m, 37.0m);
        Assert.Equal(0.5m, pair.RealizedDifference(1m));
        Assert.Throws<DomainException>(() =>
            new Money(10m, "USD").Add(new Money(10m, "VES")));
    }

    [Fact]
    public async Task Outbox_enqueue_is_transactional_and_tenant_scoped()
    {
        var tenant = Guid.NewGuid();
        await using var unitOfWork = await new TenantUnitOfWorkFactory()
            .BeginAsync(_fixture.AppConnectionString, tenant);
        var id = await new OutboxWriter().EnqueueAsync(
            unitOfWork,
            "inventory.receipt.posted",
            new { sku = "TEST", quantity = 2 },
            "corr-r2-01");
        await unitOfWork.CommitAsync();

        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT event_type, tenant_id FROM platform.outbox_events WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("inventory.receipt.posted", reader.GetString(0));
        Assert.Equal(tenant, reader.GetGuid(1));
    }
}
