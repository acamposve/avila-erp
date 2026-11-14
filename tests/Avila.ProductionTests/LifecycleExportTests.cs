using Avila.Application;
using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Platform.Lifecycle;
using Avila.Platform.Tenancy;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class LifecycleExportTests
{
    private readonly ProductionFixture _fixture;

    public LifecycleExportTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Suspended_tenant_blocks_writes_but_allows_export_and_reactivation()
    {
        var tenant = Guid.NewGuid();
        await CreateCompanyAsync(tenant, "Lifecycle Co");

        var lifecycle = new TenantLifecycleStore(_fixture.PlatformAdminConnectionString);
        var guard = new TenantAccessGuard();
        var export = new TenantExportService(new AdministrationModule());

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await guard.EnsureWritesAllowedAsync(unitOfWork);
        }

        await lifecycle.SetStatusAsync(tenant, "suspended", "Billing hold");

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await Assert.ThrowsAsync<TenantSuspendedException>(
                () => guard.EnsureWritesAllowedAsync(unitOfWork));
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            var result = await export.ExportAsync(unitOfWork);
            Assert.Single(result.Companies);
            Assert.Equal("Lifecycle Co", result.Companies[0].Name);
        }

        await lifecycle.SetStatusAsync(tenant, "active");

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await guard.EnsureWritesAllowedAsync(unitOfWork);
        }
    }

    [Fact]
    public async Task Limits_and_feature_flags_are_tenant_scoped()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var usage = new TenantUsageService();
        var flags = new TenantFeatureService();

        await using (var unitOfWork = await BeginAsync(tenantA))
        {
            await usage.ConfigureLimitAsync(unitOfWork, "users", 2);
            Assert.Equal(1, await usage.ConsumeAsync(unitOfWork, "users"));
            Assert.Equal(2, await usage.ConsumeAsync(unitOfWork, "users"));
            await Assert.ThrowsAsync<LimitExceededException>(
                () => usage.ConsumeAsync(unitOfWork, "users"));
            await unitOfWork.RollbackAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenantB))
        {
            await flags.SetAsync(unitOfWork, "beta", true);
            await usage.ConfigureLimitAsync(unitOfWork, "users", 5);
            Assert.True(await flags.IsEnabledAsync(unitOfWork, "beta"));
            Assert.Equal(1, await usage.ConsumeAsync(unitOfWork, "users"));
            await unitOfWork.CommitAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenantA))
        {
            Assert.False(await flags.IsEnabledAsync(unitOfWork, "beta"));
        }
    }

    [Fact]
    public async Task Export_is_tenant_scoped_and_cannot_include_another_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await CreateCompanyAsync(tenantA, "Company A");
        await CreateCompanyAsync(tenantB, "Company B");

        var export = new TenantExportService(new AdministrationModule());

        await using (var unitOfWork = await BeginAsync(tenantA))
        {
            var result = await export.ExportAsync(unitOfWork);
            Assert.Single(result.Companies);
            Assert.Equal("Company A", result.Companies[0].Name);
            Assert.DoesNotContain(result.Companies, company => company.Name == "Company B");
        }

        await using (var unitOfWork = await BeginAsync(tenantB))
        {
            var result = await export.ExportAsync(unitOfWork);
            Assert.Single(result.Companies);
            Assert.Equal("Company B", result.Companies[0].Name);
            Assert.DoesNotContain(result.Companies, company => company.Name == "Company A");
        }
    }

    private async Task CreateCompanyAsync(Guid tenant, string name)
    {
        await using var unitOfWork = await BeginAsync(tenant);
        await new AdministrationModule().CreateCompanyAsync(
            unitOfWork,
            new CreateCompanyCommand(
                name,
                $"J-{Guid.NewGuid():N}",
                "Av. Principal, Caracas",
                FiscalProfileComplete: true));
        await unitOfWork.CommitAsync();
    }

    private async Task<IUnitOfWork> BeginAsync(Guid tenant)
        => await new TenantUnitOfWorkFactory().BeginAsync(_fixture.AppConnectionString, tenant);
}
