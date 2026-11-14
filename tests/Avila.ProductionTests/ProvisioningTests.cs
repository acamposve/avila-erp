using Avila.Application;
using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Modules.Billing;
using Avila.Modules.Inventory;
using Avila.Modules.Sales;
using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class ProvisioningTests
{
    private readonly ProductionFixture _fixture;

    public ProvisioningTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Two_organizations_are_provisioned_without_cross_access()
    {
        var service = ProvisioningService();
        var resultA = await service.ProvisionAsync(_fixture.AppConnectionString, Request("A"));
        var resultB = await service.ProvisionAsync(_fixture.AppConnectionString, Request("B"));

        Assert.NotEqual(resultA.TenantId, resultB.TenantId);
        Assert.NotEqual(resultA.CompanyId, resultB.CompanyId);
        Assert.NotEqual(resultA.BranchId, resultB.BranchId);

        var branchesA = await ReadBranchesAsync(resultA.TenantId);
        var branchesB = await ReadBranchesAsync(resultB.TenantId);

        Assert.Single(branchesA);
        Assert.Single(branchesB);
        Assert.Equal(resultA.CompanyId, branchesA[0].CompanyId);
        Assert.Equal(resultB.CompanyId, branchesB[0].CompanyId);
        Assert.All(branchesA, branch => Assert.Equal(resultA.TenantId, branch.TenantId));
        Assert.All(branchesB, branch => Assert.Equal(resultB.TenantId, branch.TenantId));
        Assert.Equal(1, await CountSettingsAsync(resultA.TenantId));
        Assert.Equal(1, await CountSettingsAsync(resultB.TenantId));
    }

    [Fact]
    public async Task Provisioned_administrator_invitation_is_owner_scoped()
    {
        var service = ProvisioningService();
        var email = $"owner-{Guid.NewGuid():N}@example.test";
        var result = await service.ProvisionAsync(
            _fixture.AppConnectionString,
            Request("C", email));

        var identity = IdentityService();
        var accepted = await identity.AcceptInvitationAsync(
            result.AdminInvitationToken,
            "Provisioned Owner",
            "CorrectHorseBattery1!");

        Assert.Equal(result.TenantId, accepted.TenantId);
        Assert.Equal("owner", accepted.Role);

        var login = await identity.LoginAsync(
            email,
            "CorrectHorseBattery1!",
            result.TenantId);
        Assert.Equal(LoginStatus.Authenticated, login.Status);
        Assert.NotNull(login.Token);
    }

    [Fact]
    public async Task Incomplete_fiscal_profile_blocks_invoice_and_rolls_back_flow()
    {
        var flow = new SalesFlowService(
            new AdministrationModule(),
            new InventoryModule(),
            new SalesModule(),
            new BillingModule(),
            new TenantUnitOfWorkFactory());

        var tenant = Guid.NewGuid();
        var exception = await Assert.ThrowsAsync<Avila.SharedKernel.DomainException>(
            () => flow.RunAsync(
                _fixture.AppConnectionString,
                tenant,
                new SalesFlowRequest(
                    CompanyName: "Incomplete Co",
                    CompanyTaxId: $"J-{Guid.NewGuid():N}",
                    CustomerName: "Customer",
                    CustomerTaxId: "V-1",
                    ItemSku: $"SKU-{Guid.NewGuid():N}",
                    ItemName: "Item",
                    UnitPrice: 10m,
                    TaxRate: 0.16m,
                    Quantity: 1m,
                    Currency: "VES",
                    CompleteFiscalProfile: false)));

        Assert.Contains("FISCAL_PROFILE_INCOMPLETE", exception.Message);
        Assert.Equal(0, await CountCompaniesAsync(tenant));
    }

    private TenantProvisioningService ProvisioningService()
        => new(
            new AdministrationModule(),
            new TenantUnitOfWorkFactory(),
            new PlatformTenantProvisioner(_fixture.PlatformAdminConnectionString),
            IdentityService(),
            new TenantSettingsBackfill(new TenantUnitOfWorkFactory()));

    private IdentityService IdentityService()
        => new(_fixture.IdentityConnectionString, _fixture.IdentityMfaKey);

    private static ProvisionOrganizationRequest Request(string name, string? email = null)
        => new(
            OrganizationName: $"Organization {name} {Guid.NewGuid():N}",
            CompanyName: $"Company {name}",
            TaxId: $"J-{Guid.NewGuid():N}",
            FiscalAddress: "Av. Principal, Caracas",
            BranchCode: "MAIN",
            BranchName: "Sucursal Principal",
            BranchAddress: "Av. Principal, Caracas",
            AdminEmail: email ?? $"admin-{name}-{Guid.NewGuid():N}@example.test");

    private async Task<List<Branch>> ReadBranchesAsync(Guid tenantId)
    {
        await using var unitOfWork = await new TenantUnitOfWorkFactory()
            .BeginAsync(_fixture.AppConnectionString, tenantId);
        await using var context = new AdministrationDbContext(
            unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
        unitOfWork.Enlist(context);
        return await context.Branches.ToListAsync();
    }

    private async Task<int> CountCompaniesAsync(Guid tenantId)
    {
        await using var connection = new Npgsql.NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            "SELECT COUNT(*) FROM admin.companies WHERE tenant_id = @tenant",
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        return (int)(long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private async Task<int> CountSettingsAsync(Guid tenantId)
    {
        await using var connection = new Npgsql.NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            "SELECT COUNT(*) FROM platform.tenant_settings WHERE tenant_id = @tenant",
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        return (int)(long)(await command.ExecuteScalarAsync() ?? 0L);
    }
}
