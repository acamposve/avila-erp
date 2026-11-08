using Avila.Api.Tenancy;
using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Platform.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class TenantUnitOfWorkMiddlewareTests
{
    private readonly ProductionFixture _fixture;

    public TenantUnitOfWorkMiddlewareTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Middleware_commits_module_writes_for_resolved_tenant()
    {
        var tenant = Guid.NewGuid();
        var http = new DefaultHttpContext();
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(tenant);

        var middleware = new TenantUnitOfWorkMiddleware(async context =>
        {
            var unitOfWork = Assert.IsAssignableFrom<IUnitOfWork>(context.Items[typeof(IUnitOfWork)]);
            await new AdministrationModule().CreateCompanyAsync(
                unitOfWork,
                new CreateCompanyCommand("Middleware Co", "J-MIDDLEWARE-1"));
            context.Response.StatusCode = StatusCodes.Status201Created;
        });

        await middleware.InvokeAsync(
            http,
            tenantContext,
            new TenantUnitOfWorkFactory(),
            Configuration());

        Assert.Equal(StatusCodes.Status201Created, http.Response.StatusCode);
        Assert.Equal(1, await CountCompaniesAsync(tenant));
    }

    [Fact]
    public async Task Middleware_rolls_back_when_downstream_throws()
    {
        var tenant = Guid.NewGuid();
        var http = new DefaultHttpContext();
        var tenantContext = new TenantContext();
        tenantContext.SetTenant(tenant);

        var middleware = new TenantUnitOfWorkMiddleware(async context =>
        {
            var unitOfWork = Assert.IsAssignableFrom<IUnitOfWork>(context.Items[typeof(IUnitOfWork)]);
            await new AdministrationModule().CreateCompanyAsync(
                unitOfWork,
                new CreateCompanyCommand("Middleware Rollback", "J-MIDDLEWARE-2"));
            throw new InvalidOperationException("Injected middleware failure.");
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(
            http,
            tenantContext,
            new TenantUnitOfWorkFactory(),
            Configuration()));

        Assert.Equal(0, await CountCompaniesAsync(tenant));
    }

    [Fact]
    public async Task Middleware_rejects_unresolved_tenant()
    {
        var http = new DefaultHttpContext();
        var middleware = new TenantUnitOfWorkMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(
            http,
            new TenantContext(),
            new TenantUnitOfWorkFactory(),
            Configuration());

        Assert.Equal(StatusCodes.Status401Unauthorized, http.Response.StatusCode);
    }

    private IConfiguration Configuration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Avila"] = _fixture.AppConnectionString
            })
            .Build();

    private async Task<int> CountCompaniesAsync(Guid tenant)
    {
        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM admin.companies WHERE tenant_id = @tenant",
            connection);
        command.Parameters.AddWithValue("tenant", tenant);
        return (int)(long)(await command.ExecuteScalarAsync() ?? 0L);
    }
}
