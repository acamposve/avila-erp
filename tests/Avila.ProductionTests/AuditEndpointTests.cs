using Avila.Api.Audit;
using Avila.Platform.Authorization;
using Avila.Platform.Exporting;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class AuditEndpointTests
{
    private readonly ProductionFixture _fixture;

    public AuditEndpointTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Audit_query_and_export_record_audit_of_audit()
    {
        var tenant = Guid.NewGuid();
        var actor = new SessionPrincipal(Guid.NewGuid(), tenant, "owner@example.test", "owner");
        await using (var seedUnit = await new TenantUnitOfWorkFactory()
                         .BeginAsync(_fixture.AppConnectionString, tenant))
        {
            var audit = new AuditService();
            for (var index = 0; index < 2; index++)
            {
                await audit.RecordAsync(
                    seedUnit,
                    actor,
                    $"audit.endpoint.{index}",
                    "test",
                    Guid.NewGuid(),
                    before: null,
                    after: new { index },
                    correlationId: $"corr-{index}");
            }

            await seedUnit.CommitAsync();
        }

        await using var unitOfWork = await new TenantUnitOfWorkFactory()
            .BeginAsync(_fixture.AppConnectionString, tenant);
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "audit-http-corr";
        context.Request.QueryString = new QueryString("?limit=1");
        context.Response.Body = new MemoryStream();
        context.Items[typeof(SessionPrincipal)] = actor;
        context.Items[typeof(IUnitOfWork)] = unitOfWork;

        context.RequestServices = new ServiceCollection()
            .AddOptions()
            .AddLogging()
            .BuildServiceProvider();
        var auth = new AuthorizationService();
        var query = new AuditQueryService();
        var writer = new ExportAuditWriter(
            new TenantUnitOfWorkFactory(),
            _fixture.AppConnectionString);

        var result = await AuditEndpoints.QueryAsync(
            context,
            auth,
            query, writer, CancellationToken.None);
        await result.ExecuteAsync(context);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

        var exportContext = new DefaultHttpContext();
        exportContext.TraceIdentifier = "audit-export-corr";
        exportContext.Response.Body = new MemoryStream();
        exportContext.Items[typeof(SessionPrincipal)] = actor;
        exportContext.Items[typeof(IUnitOfWork)] = unitOfWork;
        await AuditEndpoints.ExportAsync(exportContext, auth, query, writer, CancellationToken.None);
        Assert.Equal(StatusCodes.Status200OK, exportContext.Response.StatusCode);
        var body = System.Text.Encoding.UTF8.GetString(
            ((MemoryStream)exportContext.Response.Body).ToArray());
        Assert.Contains("\"type\":\"manifest\"", body);

        await unitOfWork.RollbackAsync();

        var actions = await ActionsAsync(tenant);
        Assert.Contains("audit.queried", actions);
        Assert.Contains("audit.exported", actions);
    }

    private async Task<List<string>> ActionsAsync(Guid tenant)
    {
        await using var connection = new Npgsql.NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            "SELECT action FROM platform.audit_events WHERE tenant_id = @tenant ORDER BY occurred_at",
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
}

