using Avila.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class ObservabilityTests
{
    private readonly ProductionFixture _fixture;

    public ObservabilityTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Metrics_count_requests_errors_and_status_codes()
    {
        var metrics = new MetricsRegistry();
        metrics.Record(200, 10);
        metrics.Record(500, 30);

        var text = metrics.Render();

        Assert.Contains("avila_http_requests_total 2", text);
        Assert.Contains("avila_http_errors_total 1", text);
        Assert.Contains("avila_http_request_duration_ms 20.00", text);
        Assert.Contains("status=\"200\"", text);
        Assert.Contains("status=\"500\"", text);
    }

    [Fact]
    public async Task Correlation_id_is_propagated_and_invalid_values_are_replaced()
    {
        var metrics = new MetricsRegistry();
        var middleware = new RequestObservabilityMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            NullLogger<RequestObservabilityMiddleware>.Instance);

        var valid = new DefaultHttpContext();
        valid.Request.Headers[RequestObservabilityMiddleware.HeaderName] = "corr-valid_1";
        await middleware.InvokeAsync(valid, metrics);
        Assert.Equal("corr-valid_1", valid.TraceIdentifier);
        Assert.Equal("corr-valid_1", valid.Response.Headers[RequestObservabilityMiddleware.HeaderName].ToString());

        var invalid = new DefaultHttpContext();
        invalid.Request.Headers[RequestObservabilityMiddleware.HeaderName] = "bad value with spaces";
        await middleware.InvokeAsync(invalid, metrics);
        Assert.NotEqual("bad value with spaces", invalid.TraceIdentifier);
        Assert.Equal(32, invalid.TraceIdentifier.Length);
    }

    [Fact]
    public async Task Database_health_check_reports_reachable_and_missing_configuration()
    {
        var configured = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Platform"] = _fixture.AppConnectionString
            })
            .Build();

        var healthy = await new DatabaseHealthCheck(configured).CheckHealthAsync(
            new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, healthy.Status);

        var missing = new ConfigurationBuilder().Build();
        var degraded = await new DatabaseHealthCheck(missing).CheckHealthAsync(
            new HealthCheckContext());
        Assert.Equal(HealthStatus.Degraded, degraded.Status);
    }
}
