using System.Text;
using System.Text.Json;
using Avila.Api.Exporting;
using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Platform.Exporting;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class ExportEndpointTests
{
    private readonly ProductionFixture _fixture;

    public ExportEndpointTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Owner_export_streams_ndjson_and_audits_requested_and_completed()
    {
        var tenant = Guid.NewGuid();
        await CreateCompanyAsync(tenant, "Export Co");
        var (context, unitOfWork) = await CreateContextAsync(tenant, "owner", stream: true);
        await using var unit = unitOfWork;
        var registry = Registry();
        var auditWriter = Audit();

        await TenantExportEndpoint.HandleAsync(context, registry, auditWriter);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.StartsWith("application/x-ndjson", context.Response.ContentType);
        var lines = ReadLines(context);
        Assert.Contains(lines, line => line.Contains("\"type\":\"export\""));
        Assert.Contains(lines, line => line.Contains("\"type\":\"manifest\""));
        Assert.Contains(lines, line => line.Contains("\"module\":\"administration\""));
        Assert.Contains("Export Co", string.Join('\n', lines));

        var actions = await AuditActionsAsync(tenant);
        Assert.Contains("tenant.export.requested", actions);
        Assert.Contains("tenant.export.completed", actions);
        Assert.DoesNotContain("tenant.export.failed", actions);

        await unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task Export_requires_recent_step_up()
    {
        var tenant = Guid.NewGuid();
        await CreateCompanyAsync(tenant, "Step Up Co");
        var (context, unitOfWork) = await CreateContextAsync(
            tenant,
            "owner",
            stream: true,
            steppedUp: false);
        await using var unit = unitOfWork;

        await TenantExportEndpoint.HandleAsync(context, Registry(), Audit());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Contains("STEP_UP_REQUIRED", await ReadBodyAsync(context));
        await unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task Member_export_is_forbidden_and_does_not_start_stream()
    {
        var tenant = Guid.NewGuid();
        await CreateCompanyAsync(tenant, "Member Co");
        var (context, unitOfWork) = await CreateContextAsync(tenant, "member", stream: true);
        await using var unit = unitOfWork;

        await TenantExportEndpoint.HandleAsync(context, Registry(), Audit());

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Empty(ReadLines(context));
        await unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task Caller_supplied_tenant_override_is_rejected()
    {
        var tenant = Guid.NewGuid();
        await CreateCompanyAsync(tenant, "Override Co");
        var (context, unitOfWork) = await CreateContextAsync(tenant, "owner", stream: true);
        await using var unit = unitOfWork;
        context.Request.QueryString = new QueryString($"?tenantId={Guid.NewGuid():D}");

        await TenantExportEndpoint.HandleAsync(context, Registry(), Audit());

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        await unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task Suspended_tenant_can_still_export()
    {
        var tenant = Guid.NewGuid();
        await CreateCompanyAsync(tenant, "Suspended Co");
        var lifecycle = new Avila.Platform.Lifecycle.TenantLifecycleStore(
            _fixture.PlatformAdminConnectionString);
        await lifecycle.SetStatusAsync(tenant, "suspended", "Portability request");

        var (context, unitOfWork) = await CreateContextAsync(tenant, "owner", stream: true);
        await using var unit = unitOfWork;
        await TenantExportEndpoint.HandleAsync(context, Registry(), Audit());

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("Suspended Co", string.Join('\n', ReadLines(context)));
        await unitOfWork.RollbackAsync();
    }

    [Fact]
    public async Task Failed_export_is_audited_and_returns_500_when_stream_not_started()
    {
        var tenant = Guid.NewGuid();
        var (context, unitOfWork) = await CreateContextAsync(tenant, "owner", stream: true);
        await using var unit = unitOfWork;
        var registry = new TenantExportRegistry([new ThrowingContributor()]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => TenantExportEndpoint.HandleAsync(context, registry, Audit()));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var actions = await AuditActionsAsync(tenant);
        Assert.Contains("tenant.export.requested", actions);
        Assert.Contains("tenant.export.failed", actions);
        Assert.DoesNotContain("tenant.export.completed", actions);
        await unitOfWork.RollbackAsync();
    }

    private TenantExportRegistry Registry()
        => new([new AdministrationExportContributor(new AdministrationModule())]);

    private ExportAuditWriter Audit()
        => new(new TenantUnitOfWorkFactory(), _fixture.AppConnectionString);

    private async Task<(DefaultHttpContext Context, IUnitOfWork UnitOfWork)> CreateContextAsync(
        Guid tenant,
        string role,
        bool stream,
        bool steppedUp = true)
    {
        var unitOfWork = await new TenantUnitOfWorkFactory()
            .BeginAsync(_fixture.AppConnectionString, tenant);
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "export-corr";
        context.Items[typeof(SessionPrincipal)] = new SessionPrincipal(
            Guid.NewGuid(),
            tenant,
            "actor@example.test",
            role,
            SteppedUpAt: steppedUp ? DateTimeOffset.UtcNow : null);
        context.Items[typeof(IUnitOfWork)] = unitOfWork;
        context.Response.Body = new MemoryStream();
        return (context, unitOfWork);
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(
            context.Response.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static List<string> ReadLines(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(
            context.Response.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    private async Task CreateCompanyAsync(Guid tenant, string name)
    {
        await using var unitOfWork = await new TenantUnitOfWorkFactory()
            .BeginAsync(_fixture.AppConnectionString, tenant);
        await new AdministrationModule().CreateCompanyAsync(
            unitOfWork,
            new CreateCompanyCommand(
                name,
                $"J-{Guid.NewGuid():N}"[..10],
                "Av. Principal, Caracas",
                FiscalProfileComplete: true));
        await unitOfWork.CommitAsync();
    }

    private async Task<List<string>> AuditActionsAsync(Guid tenant)
    {
        await using var connection = new Npgsql.NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            """
            SELECT action
            FROM platform.audit_events
            WHERE tenant_id = @tenant
              AND entity_type = 'tenant_export'
            ORDER BY occurred_at
            """,
            connection);
        command.Parameters.AddWithValue("tenant", tenant);
        await using var reader = await command.ExecuteReaderAsync();
        var actions = new List<string>();
        while (await reader.ReadAsync())
        {
            actions.Add(reader.GetString(0));
        }

        return actions;
    }

    private sealed class ThrowingContributor : ITenantExportContributor
    {
        public string ModuleKey => "throwing";
        public int Order => 100;
        public int SchemaVersion => 1;

        public Task<TenantExportSection> ExportAsync(
            IUnitOfWork unitOfWork,
            TenantExportContext context,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Injected export failure.");
    }
}


