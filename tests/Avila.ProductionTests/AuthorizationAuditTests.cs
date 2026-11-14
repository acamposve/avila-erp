using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Platform.Authorization;
using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class AuthorizationAuditTests
{
    private readonly ProductionFixture _fixture;

    public AuthorizationAuditTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Member_without_scope_is_denied_without_side_effects()
    {
        var tenant = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var company = await CreateCompanyAsync(tenant, "Denied Co");
        var authorization = new AuthorizationService();
        var member = new SessionPrincipal(userId, tenant, "member@example.test", "member");

        await using var unitOfWork = await BeginAsync(tenant);
        var allowed = await authorization.IsAllowedAsync(
            unitOfWork,
            member,
            Permissions.SalesQuoteCreate,
            AccessScopeRef.Company(company.Id));

        Assert.False(allowed);
        await Assert.ThrowsAsync<AuthorizationException>(() => authorization.EnsureAllowedAsync(
            unitOfWork,
            member,
            Permissions.SalesQuoteCreate,
            AccessScopeRef.Company(company.Id)));

        Assert.Equal(0, await CountQuotesAsync(tenant));
    }

    [Fact]
    public async Task Member_with_company_scope_is_allowed_only_for_that_company()
    {
        var tenant = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var companyA = await CreateCompanyAsync(tenant, "Company A");
        var companyB = await CreateCompanyAsync(tenant, "Company B");
        var owner = new SessionPrincipal(Guid.NewGuid(), tenant, "owner@example.test", "owner");
        var member = new SessionPrincipal(userId, tenant, "member@example.test", "member");
        var authorization = new AuthorizationService();

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await authorization.AssignScopeAsync(
                unitOfWork,
                owner,
                userId,
                AccessScopeRef.Company(companyA.Id));
            await unitOfWork.CommitAsync();
        }

        await using var readUnitOfWork = await BeginAsync(tenant);
        Assert.True(await authorization.IsAllowedAsync(
            readUnitOfWork,
            member,
            Permissions.SalesQuoteCreate,
            AccessScopeRef.Company(companyA.Id)));
        Assert.False(await authorization.IsAllowedAsync(
            readUnitOfWork,
            member,
            Permissions.SalesQuoteCreate,
            AccessScopeRef.Company(companyB.Id)));
        Assert.False(await authorization.IsAllowedAsync(
            readUnitOfWork,
            member,
            Permissions.InventoryStockAdjust,
            AccessScopeRef.Company(companyA.Id)));
    }

    [Fact]
    public async Task Audit_event_is_tenant_scoped_and_append_only()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var actor = new SessionPrincipal(Guid.NewGuid(), tenantA, "actor@example.test", "owner");
        var audit = new AuditService();
        Guid auditId;

        await using (var unitOfWork = await BeginAsync(tenantA))
        {
            auditId = await audit.RecordAsync(
                unitOfWork,
                actor,
                "company.updated",
                "company",
                Guid.NewGuid(),
                before: new { Name = "Old" },
                after: new { Name = "New" },
                correlationId: "corr-1");
            await unitOfWork.CommitAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenantA))
        {
            await using var context = new PlatformDbContext(
                unitOfWork.OptionsFor<PlatformDbContext>("platform"));
            unitOfWork.Enlist(context);
            var row = await context.AuditEvents.SingleAsync(x => x.Id == auditId);
            Assert.Equal("company.updated", row.Action);
            Assert.Equal(actor.UserId, row.ActorUserId);
            Assert.Equal("corr-1", row.CorrelationId);
            Assert.Contains("Old", row.BeforeJson);
            Assert.Contains("New", row.AfterJson);
        }

        await using (var unitOfWork = await BeginAsync(tenantB))
        {
            await using var context = new PlatformDbContext(
                unitOfWork.OptionsFor<PlatformDbContext>("platform"));
            unitOfWork.Enlist(context);
            Assert.Empty(await context.AuditEvents.ToListAsync());
        }

        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var update = new NpgsqlCommand(
            "UPDATE platform.audit_events SET action = 'tampered' WHERE id = @id",
            connection);
        update.Parameters.AddWithValue("id", auditId);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            async () => await update.ExecuteNonQueryAsync());
        Assert.Equal("42501", exception.SqlState);

        await using var admin = new NpgsqlConnection(_fixture.AdminConnectionString);
        await admin.OpenAsync();
        await using var verify = new NpgsqlCommand(
            "SELECT action FROM platform.audit_events WHERE id = @id",
            admin);
        verify.Parameters.AddWithValue("id", auditId);
        Assert.Equal("company.updated", (string?)await verify.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Support_access_requires_ticket_and_expires_and_is_audited()
    {
        var tenant = Guid.NewGuid();
        var actor = new SessionPrincipal(Guid.NewGuid(), tenant, "owner@example.test", "owner");
        var supportUserId = Guid.NewGuid();
        var audit = new AuditService();
        var service = new SupportAccessService(audit);

        Guid grantId;
        await using (var unitOfWork = await BeginAsync(tenant))
        {
            grantId = await service.GrantAsync(
                unitOfWork,
                actor,
                supportUserId,
                "TCK-100",
                "Investigating a customer report",
                DateTimeOffset.UtcNow.AddHours(1));
            await unitOfWork.CommitAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            Assert.True(await service.HasAccessAsync(unitOfWork, supportUserId));
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await using var context = new PlatformDbContext(
                unitOfWork.OptionsFor<PlatformDbContext>("platform"));
            unitOfWork.Enlist(context);
            Assert.Single(await context.AuditEvents
                .Where(x => x.Action == "support.access.granted")
                .ToListAsync());
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await service.RevokeAsync(unitOfWork, actor, grantId);
            await unitOfWork.CommitAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            Assert.False(await service.HasAccessAsync(unitOfWork, supportUserId));
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await Assert.ThrowsAsync<Avila.SharedKernel.DomainException>(() => service.GrantAsync(
                unitOfWork,
                actor,
                supportUserId,
                "",
                "No ticket",
                DateTimeOffset.UtcNow.AddHours(1)));
            await unitOfWork.RollbackAsync();
        }
    }

    [Fact]
    public async Task Cross_tenant_principal_is_denied()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var company = await CreateCompanyAsync(tenantA, "Cross Co");
        var principal = new SessionPrincipal(Guid.NewGuid(), tenantB, "other@example.test", "owner");
        var authorization = new AuthorizationService();

        await using var unitOfWork = await BeginAsync(tenantA);
        Assert.False(await authorization.IsAllowedAsync(
            unitOfWork,
            principal,
            Permissions.SalesQuoteCreate,
            AccessScopeRef.Company(company.Id)));
    }

    private async Task<CompanyDto> CreateCompanyAsync(Guid tenant, string name)
    {
        await using var unitOfWork = await BeginAsync(tenant);
        var company = await new AdministrationModule().CreateCompanyAsync(
            unitOfWork,
            new CreateCompanyCommand(name, $"J-{Guid.NewGuid():N}", "Av. Principal, Caracas", true));
        await unitOfWork.CommitAsync();
        return company;
    }

    private async Task<IUnitOfWork> BeginAsync(Guid tenant)
        => await new TenantUnitOfWorkFactory().BeginAsync(_fixture.AppConnectionString, tenant);

    private async Task<int> CountQuotesAsync(Guid tenant)
    {
        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM sales.quotes WHERE tenant_id = @tenant",
            connection);
        command.Parameters.AddWithValue("tenant", tenant);
        return (int)(long)(await command.ExecuteScalarAsync() ?? 0L);
    }
}
