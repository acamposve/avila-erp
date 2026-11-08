using Avila.Spike.Tenancy;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class TenantIsolationTests
{
    private readonly PostgresFixture _fixture;

    public TenantIsolationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T07_pool_reuse_does_not_leak_tenant_context()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await TestDb.SeedInvoiceAsync(_fixture.AdminConnectionString, tenantA, number: 1);
        await TestDb.SeedInvoiceAsync(_fixture.AdminConnectionString, tenantB, number: 1);

        var singleConnectionPool = TenantSession.WithMaxPoolSize(_fixture.AppConnectionString, 1);

        var (tenantsA, pidA) = await TestDb.QueryInvoiceTenantsAsync(singleConnectionPool, tenantA);
        var (tenantsB, pidB) = await TestDb.QueryInvoiceTenantsAsync(singleConnectionPool, tenantB);
        var (tenantsWithoutContext, pidWithoutContext) = await TestDb.QueryInvoiceTenantsAsync(
            singleConnectionPool,
            tenantId: null);

        Assert.Equal(new[] { tenantA }, tenantsA);
        Assert.Equal(new[] { tenantB }, tenantsB);
        Assert.Empty(tenantsWithoutContext);
        Assert.Equal(pidA, pidB);
        Assert.True(pidA > 0);
        Assert.Equal(-1, pidWithoutContext);
    }
}
