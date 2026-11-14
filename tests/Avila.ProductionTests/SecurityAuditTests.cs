using Avila.Platform.Authorization;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;
using Npgsql;
using System.Text.Json;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class SecurityAuditTests
{
    private readonly ProductionFixture _fixture;

    public SecurityAuditTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Denied_permission_is_audited_after_business_transaction_rolls_back()
    {
        var tenant = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var writer = new SecurityAuditWriter(
            new TenantUnitOfWorkFactory(),
            _fixture.AppConnectionString);
        var authorization = new AuthorizationService(writer);
        var member = new SessionPrincipal(actorId, tenant, "member@example.test", "member");

        await using (var unitOfWork = await new TenantUnitOfWorkFactory()
                         .BeginAsync(_fixture.AppConnectionString, tenant))
        {
            await Assert.ThrowsAsync<AuthorizationException>(() => authorization.EnsureAllowedAsync(
                unitOfWork,
                member,
                Permissions.InventoryStockAdjust,
                AccessScopeRef.Company(Guid.NewGuid()),
                "denied-permission-corr"));

            // The caller's business transaction is deliberately discarded.
            await unitOfWork.RollbackAsync();
        }

        var eventRow = await ReadDeniedEventAsync(tenant, actorId);
        Assert.Equal("authorization.denied", eventRow.Action);
        Assert.Equal("denied-permission-corr", eventRow.CorrelationId);
        using var payload = JsonDocument.Parse(eventRow.AfterJson);
        Assert.Equal("permission", payload.RootElement.GetProperty("reason").GetString());
        Assert.Equal(tenant.ToString("D"), payload.RootElement.GetProperty("actorTenantId").GetString());
    }

    [Fact]
    public async Task Cross_tenant_attempt_is_audited_in_target_tenant_after_rollback()
    {
        var targetTenant = Guid.NewGuid();
        var actorTenant = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var writer = new SecurityAuditWriter(
            new TenantUnitOfWorkFactory(),
            _fixture.AppConnectionString);
        var authorization = new AuthorizationService(writer);
        var foreignPrincipal = new SessionPrincipal(
            actorId,
            actorTenant,
            "foreign@example.test",
            "owner");

        await using (var unitOfWork = await new TenantUnitOfWorkFactory()
                         .BeginAsync(_fixture.AppConnectionString, targetTenant))
        {
            await Assert.ThrowsAsync<AuthorizationException>(() => authorization.EnsureAllowedAsync(
                unitOfWork,
                foreignPrincipal,
                Permissions.SalesQuoteCreate,
                AccessScopeRef.Company(Guid.NewGuid()),
                "cross-tenant-corr"));
            await unitOfWork.RollbackAsync();
        }

        var eventRow = await ReadDeniedEventAsync(targetTenant, actorId);
        Assert.Equal("authorization.denied", eventRow.Action);
        Assert.Equal("cross-tenant-corr", eventRow.CorrelationId);
        using var payload = JsonDocument.Parse(eventRow.AfterJson);
        Assert.Equal("cross_tenant", payload.RootElement.GetProperty("reason").GetString());
        Assert.Equal(actorTenant.ToString("D"), payload.RootElement.GetProperty("actorTenantId").GetString());
    }

    private async Task<(string Action, string CorrelationId, string AfterJson)> ReadDeniedEventAsync(
        Guid tenantId,
        Guid actorId)
    {
        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT action, correlation_id, after_json::text
            FROM platform.audit_events
            WHERE tenant_id = @tenant
              AND actor_user_id = @actor
              AND action = 'authorization.denied'
            ORDER BY occurred_at DESC
            LIMIT 1
            """,
            connection);
        command.Parameters.AddWithValue("tenant", tenantId);
        command.Parameters.AddWithValue("actor", actorId);

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetString(0), reader.GetString(1), reader.GetString(2));
    }
}
