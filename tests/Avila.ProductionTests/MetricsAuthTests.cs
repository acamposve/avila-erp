using System.Net;
using Avila.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Avila.ProductionTests;

public sealed class MetricsAuthTests
{
    [Fact]
    public async Task Metrics_allow_loopback_by_default()
    {
        var context = Context(IPAddress.Loopback);
        var config = Configuration();

        await MetricsEndpoint.HandleAsync(context, new MetricsRegistry(), config);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task Metrics_reject_remote_without_token_or_allowlist()
    {
        var context = Context(IPAddress.Parse("203.0.113.10"));
        var config = Configuration();

        await MetricsEndpoint.HandleAsync(context, new MetricsRegistry(), config);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task Metrics_allow_remote_with_token()
    {
        var context = Context(IPAddress.Parse("203.0.113.10"));
        context.Request.Headers.Authorization = "Bearer metrics-token";
        var config = Configuration(("Observability:MetricsToken", "metrics-token"));

        await MetricsEndpoint.HandleAsync(context, new MetricsRegistry(), config);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task Metrics_allow_exact_network_allowlist()
    {
        var context = Context(IPAddress.Parse("203.0.113.10"));
        var config = Configuration(
            ("Observability:MetricsAllowedNetworks", "198.51.100.1,203.0.113.10"));

        await MetricsEndpoint.HandleAsync(context, new MetricsRegistry(), config);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static DefaultHttpContext Context(IPAddress address)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = address;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(x => x.Key, x => (string?)x.Value))
            .Build();
}
