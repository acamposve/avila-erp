using Avila.Spike.Tenancy;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class ReadOnlyRlsTests
{
    private readonly PostgresFixture _fixture;

    public ReadOnlyRlsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T08_read_only_transaction_keeps_rls_context()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await TestDb.SeedInvoiceAsync(_fixture.AdminConnectionString, tenantA, number: 1);
        await TestDb.SeedInvoiceAsync(_fixture.AdminConnectionString, tenantB, number: 1);

        var (tenantsA, _) = await TestDb.QueryInvoiceTenantsReadOnlyAsync(
            _fixture.AppConnectionString,
            tenantA);
        var (tenantsB, _) = await TestDb.QueryInvoiceTenantsReadOnlyAsync(
            _fixture.AppConnectionString,
            tenantB);
        var (tenantsWithoutContext, _) = await TestDb.QueryInvoiceTenantsReadOnlyAsync(
            _fixture.AppConnectionString,
            tenantId: null);

        Assert.Equal(new[] { tenantA }, tenantsA);
        Assert.Equal(new[] { tenantB }, tenantsB);
        Assert.Empty(tenantsWithoutContext);
    }
}
