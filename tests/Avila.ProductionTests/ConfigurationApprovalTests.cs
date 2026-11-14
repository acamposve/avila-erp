using Avila.Platform.Approvals;
using Avila.Platform.Authorization;
using Avila.Platform.Configuration;
using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class ConfigurationApprovalTests
{
    private readonly ProductionFixture _fixture;

    public ConfigurationApprovalTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Setting_versions_select_by_effective_date_and_preserve_history()
    {
        var tenant = Guid.NewGuid();
        var owner = new SessionPrincipal(Guid.NewGuid(), tenant, "owner@example.test", "owner");
        var service = Configuration();

        var v1 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var v2 = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var v3 = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await service.PublishAsync(unitOfWork, owner, "tax.rate", "0.16", "internal-test", v1, v2);
            await service.PublishAsync(unitOfWork, owner, "tax.rate", "0.17", "internal-test", v2, v3);
            await unitOfWork.CommitAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            var march = await service.GetEffectiveAsync(unitOfWork, "tax.rate", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
            var july = await service.GetEffectiveAsync(unitOfWork, "tax.rate", new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero));
            Assert.Equal("0.16", march?.Value);
            Assert.Equal("0.17", july?.Value);

            var history = await service.GetHistoryAsync(unitOfWork, "tax.rate");
            Assert.Equal(2, history.Count);
            Assert.Equal(["internal-test", "internal-test"], history.Select(x => x.Source).ToArray());
        }

        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var update = new NpgsqlCommand(
            "UPDATE platform.setting_versions SET value = 'tampered' WHERE key = 'tax.rate'",
            connection);
        var exception = await Assert.ThrowsAsync<PostgresException>(
            async () => await update.ExecuteNonQueryAsync());
        Assert.Equal("42501", exception.SqlState);
    }

    [Fact]
    public async Task Member_cannot_publish_settings()
    {
        var tenant = Guid.NewGuid();
        var member = new SessionPrincipal(Guid.NewGuid(), tenant, "member@example.test", "member");
        var service = Configuration();

        await using var unitOfWork = await BeginAsync(tenant);
        await Assert.ThrowsAsync<AuthorizationException>(() => service.PublishAsync(
            unitOfWork,
            member,
            "tax.rate",
            "0.16",
            "internal-test",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(1)));
        await unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task Missing_approval_blocks_operation_and_segregation_is_enforced()
    {
        var tenant = Guid.NewGuid();
        var requester = new SessionPrincipal(Guid.NewGuid(), tenant, "requester@example.test", "owner");
        var approver = new SessionPrincipal(Guid.NewGuid(), tenant, "approver@example.test", "owner");
        var entityId = Guid.NewGuid();
        var service = Approvals();

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await service.ConfigureRuleAsync(unitOfWork, requester, "purchase", 1000m);
            await unitOfWork.CommitAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await Assert.ThrowsAsync<ApprovalRequiredException>(() => service.EnsureApprovedAsync(
                unitOfWork,
                "purchase",
                Guid.NewGuid(),
                1500m));
            await Assert.ThrowsAsync<ApprovalRequiredException>(() => service.EnsureApprovedAsync(
                unitOfWork,
                "purchase",
                Guid.NewGuid(),
                1500m));
        }

        Guid requestId;
        await using (var unitOfWork = await BeginAsync(tenant))
        {
            requestId = await service.RequestAsync(
                unitOfWork,
                requester,
                "purchase",
                entityId,
                1500m,
                "Over threshold");
            await unitOfWork.CommitAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await Assert.ThrowsAsync<Avila.SharedKernel.DomainException>(() => service.DecideAsync(
                unitOfWork,
                requester,
                requestId,
                true,
                "Self approval"));
            await unitOfWork.RollbackAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await service.DecideAsync(unitOfWork, approver, requestId, true, "Approved by second owner");
            await unitOfWork.CommitAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await service.EnsureApprovedAsync(unitOfWork, "purchase", entityId, 1500m);
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await using var context = new PlatformDbContext(
                unitOfWork.OptionsFor<PlatformDbContext>("platform"));
            unitOfWork.Enlist(context);
            var actions = await context.AuditEvents.Select(x => x.Action).ToListAsync();
            Assert.Contains("approval.requested", actions);
            Assert.Contains("approval.approved", actions);
        }
    }

    [Fact]
    public async Task Setting_and_approval_changes_are_audited()
    {
        var tenant = Guid.NewGuid();
        var owner = new SessionPrincipal(Guid.NewGuid(), tenant, "owner@example.test", "owner");
        var service = Configuration();

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await service.SetPreferenceAsync(unitOfWork, owner, "locale", "es-VE");
            await service.PublishAsync(
                unitOfWork,
                owner,
                "inventory.cost.method",
                "average",
                "internal-test",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(30));
            await unitOfWork.CommitAsync();
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            await using var context = new PlatformDbContext(
                unitOfWork.OptionsFor<PlatformDbContext>("platform"));
            unitOfWork.Enlist(context);
            var actions = await context.AuditEvents.Select(x => x.Action).ToListAsync();
            Assert.Contains("tenant.preference.updated", actions);
            Assert.Contains("setting.version.published", actions);
        }
    }

    [Fact]
    public async Task Approval_without_rule_does_not_require_approval()
    {
        var tenant = Guid.NewGuid();
        var service = Approvals();

        await using var unitOfWork = await BeginAsync(tenant);
        await service.EnsureApprovedAsync(unitOfWork, "purchase", Guid.NewGuid(), 500m);
    }

    private ConfigurationService Configuration()
        => new(new AuthorizationService(), new AuditService());

    private ApprovalService Approvals()
        => new(new AuthorizationService(), new AuditService());

    private async Task<IUnitOfWork> BeginAsync(Guid tenant)
        => await new TenantUnitOfWorkFactory().BeginAsync(_fixture.AppConnectionString, tenant);
}
