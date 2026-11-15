using Avila.Platform.Authorization;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;
using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class AuditIntegrityTests
{
    private readonly ProductionFixture _fixture;

    public AuditIntegrityTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Chain_verifies_and_detects_tampering_or_deletion()
    {
        var tenant = Guid.NewGuid();
        var ids = await RecordThreeAsync(tenant);

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            var result = await new AuditIntegrityService().VerifyAsync(unitOfWork);
            Assert.True(result.IsValid);
            Assert.Equal(3, result.CheckedEvents);
        }

        await using (var connection = new NpgsqlConnection(_fixture.AdminConnectionString))
        {
            await connection.OpenAsync();
            await using (var disable = new NpgsqlCommand(
                "ALTER TABLE platform.audit_events DISABLE TRIGGER audit_events_append_only",
                connection))
            {
                await disable.ExecuteNonQueryAsync();
            }

            await using (var tamper = new NpgsqlCommand(
                "UPDATE platform.audit_events SET action = 'tampered' WHERE id = @id",
                connection))
            {
                tamper.Parameters.AddWithValue("id", ids[1]);
                await tamper.ExecuteNonQueryAsync();
            }

            await using (var enable = new NpgsqlCommand(
                "ALTER TABLE platform.audit_events ENABLE TRIGGER audit_events_append_only",
                connection))
            {
                await enable.ExecuteNonQueryAsync();
            }
        }

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            var result = await new AuditIntegrityService().VerifyAsync(unitOfWork);
            Assert.False(result.IsValid);
            Assert.Contains("event_hash", result.Reason);
        }
    }

    [Fact]
    public async Task Delete_breaks_the_chain()
    {
        var tenant = Guid.NewGuid();
        var ids = await RecordThreeAsync(tenant);

        await using (var connection = new NpgsqlConnection(_fixture.AdminConnectionString))
        {
            await connection.OpenAsync();
            await using (var disable = new NpgsqlCommand(
                "ALTER TABLE platform.audit_events DISABLE TRIGGER audit_events_append_only",
                connection))
            {
                await disable.ExecuteNonQueryAsync();
            }

            await using (var delete = new NpgsqlCommand(
                "DELETE FROM platform.audit_events WHERE id = @id",
                connection))
            {
                delete.Parameters.AddWithValue("id", ids[1]);
                await delete.ExecuteNonQueryAsync();
            }

            await using (var enable = new NpgsqlCommand(
                "ALTER TABLE platform.audit_events ENABLE TRIGGER audit_events_append_only",
                connection))
            {
                await enable.ExecuteNonQueryAsync();
            }
        }

        await using var unitOfWork = await BeginAsync(tenant);
        var result = await new AuditIntegrityService().VerifyAsync(unitOfWork);
        Assert.False(result.IsValid);
        Assert.Contains("previous_hash", result.Reason);
    }

    [Fact]
    public async Task Query_paginates_and_export_is_tenant_scoped()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await RecordThreeAsync(tenantA);
        await RecordThreeAsync(tenantB);

        var service = new AuditQueryService();
        await using var unitOfWork = await BeginAsync(tenantA);
        var first = await service.QueryAsync(unitOfWork, new AuditQuery(), limit: 2);
        Assert.Equal(2, first.Items.Count);
        Assert.NotNull(first.NextCursor);
        Assert.All(first.Items, item => Assert.Equal(tenantA, item.TenantId));

        var second = await service.QueryAsync(
            unitOfWork,
            new AuditQuery(),
            first.NextCursor,
            2);
        Assert.Single(second.Items);
        Assert.Null(second.NextCursor);

        await using var output = new MemoryStream();
        var count = await service.ExportNdjsonAsync(unitOfWork, new AuditQuery(), output);
        Assert.Equal(3, count);
        var text = System.Text.Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("\"type\":\"audit\"", text);
        Assert.DoesNotContain(tenantB.ToString("D"), text);
    }

    private async Task<List<Guid>> RecordThreeAsync(Guid tenant)
    {
        var ids = new List<Guid>();
        await using var unitOfWork = await BeginAsync(tenant);
        var actor = new SessionPrincipal(Guid.NewGuid(), tenant, "audit@example.test", "owner");
        var audit = new AuditService();
        for (var index = 0; index < 3; index++)
        {
            ids.Add(await audit.RecordAsync(
                unitOfWork,
                actor,
                $"audit.test.{index}",
                "test",
                Guid.NewGuid(),
                before: new { value = index },
                after: new { value = index + 1 },
                correlationId: $"corr-{index}"));
        }

        await unitOfWork.CommitAsync();
        return ids;
    }

    private async Task<IUnitOfWork> BeginAsync(Guid tenant)
        => await new TenantUnitOfWorkFactory().BeginAsync(_fixture.AppConnectionString, tenant);
}
