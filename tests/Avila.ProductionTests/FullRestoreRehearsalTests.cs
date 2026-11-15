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
public sealed class FullRestoreRehearsalTests
{
    private readonly ProductionFixture _fixture;

    public FullRestoreRehearsalTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Full_schema_dump_restore_preserves_rls_counts_and_audit_chain()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await SeedAsync(tenantA, "Restore A");
        await SeedAsync(tenantB, "Restore B");

        var dump = "/tmp/avila-rem13.dump";
        await ExecAsync("pg_dump", "-U", "postgres", "-d", "avila", "-Fc", "-f", dump);
        await ExecAsync("createdb", "-U", "postgres", "avila_restore");
        await ExecAsync("pg_restore", "-U", "postgres", "-d", "avila_restore", dump);

        var restoreAdmin = new NpgsqlConnectionStringBuilder(_fixture.AdminConnectionString)
        {
            Database = "avila_restore"
        }.ConnectionString;
        var restoreApp = new NpgsqlConnectionStringBuilder(_fixture.AppConnectionString)
        {
            Database = "avila_restore"
        }.ConnectionString;

        await using (var connection = new NpgsqlConnection(restoreAdmin))
        {
            await connection.OpenAsync();
            Assert.Equal(1L, await ScalarAsync(connection,
                "SELECT COUNT(*) FROM admin.companies WHERE tenant_id = @tenant", tenantA));
            Assert.Equal(1L, await ScalarAsync(connection,
                "SELECT COUNT(*) FROM admin.companies WHERE tenant_id = @tenant", tenantB));
            Assert.Equal(2L, await ScalarAsync(connection,
                "SELECT COUNT(*) FROM platform.audit_events WHERE action LIKE 'restore.test%'", null));
            Assert.True(await ScalarAsync(connection,
                """
                SELECT COUNT(*)
                FROM platform.rls_policy_registry r
                JOIN pg_namespace n ON n.nspname = r.table_schema
                JOIN pg_class c ON c.relnamespace = n.oid AND c.relname = r.table_name
                WHERE c.relrowsecurity AND c.relforcerowsecurity
                  AND EXISTS (SELECT 1 FROM pg_policy p WHERE p.polrelid = c.oid AND p.polname = r.policy_name)
                """, null) > 0);
        }

        await using (var unitOfWork = await new TenantUnitOfWorkFactory()
                         .BeginAsync(restoreApp, tenantA))
        {
            await using var context = new AdministrationDbContext(
                unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
            unitOfWork.Enlist(context);
            var companies = await context.Companies.AsNoTracking().ToListAsync();
            Assert.Single(companies);
            Assert.Equal(tenantA, companies[0].TenantId);
        }

        await using (var unitOfWork = await new TenantUnitOfWorkFactory()
                         .BeginAsync(restoreApp, tenantA))
        {
            var result = await new AuditIntegrityService().VerifyAsync(unitOfWork);
            Assert.True(result.IsValid);
            Assert.Equal(1, result.CheckedEvents);
        }
    }

    private async Task SeedAsync(Guid tenant, string companyName)
    {
        await using var unitOfWork = await new TenantUnitOfWorkFactory()
            .BeginAsync(_fixture.AppConnectionString, tenant);
        var module = new AdministrationModule();
        var company = await module.CreateCompanyAsync(
            unitOfWork,
            new CreateCompanyCommand(
                companyName,
                $"J-{Guid.NewGuid():N}"[..10],
                "Av. Principal, Caracas",
                FiscalProfileComplete: true));
        await module.CreateBranchAsync(
            unitOfWork,
            new CreateBranchCommand(company.Id, $"B-{Guid.NewGuid():N}"[..10], "Main"));
        await module.CreatePartyAsync(
            unitOfWork,
            new CreatePartyCommand("Restore Party", $"V-{Guid.NewGuid():N}"[..10], true, false));
        await new AuditService().RecordAsync(
            unitOfWork,
            new SessionPrincipal(Guid.NewGuid(), tenant, "owner@example.test", "owner"),
            "restore.test",
            "restore",
            Guid.NewGuid(),
            before: null,
            after: new { company = companyName },
            correlationId: Guid.NewGuid().ToString("N"));
        await unitOfWork.CommitAsync();
    }

    private async Task ExecAsync(params string[] command)
    {
        var result = await _fixture.Container.ExecAsync(command);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command {string.Join(' ', command)} failed: {result.Stderr}");
        }
    }

    private static async Task<long> ScalarAsync(
        NpgsqlConnection connection,
        string sql,
        Guid? tenant)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        if (tenant.HasValue)
        {
            command.Parameters.AddWithValue("tenant", tenant.Value);
        }

        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }
}
