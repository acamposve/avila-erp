using System.Net;

namespace Avila.Api.Observability;

public static class MetricsEndpoint
{
    public static async Task HandleAsync(
        HttpContext context,
        MetricsRegistry metrics,
        IConfiguration configuration)
    {
        if (!IsAllowed(context, configuration))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.Response.ContentType = "text/plain; version=0.0.4";
        await context.Response.WriteAsync(metrics.Render());
    }

    private static bool IsAllowed(HttpContext context, IConfiguration configuration)
    {
        var token = configuration["Observability:MetricsToken"];
        var authorization = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(token)
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                authorization["Bearer ".Length..].Trim(),
                token,
                StringComparison.Ordinal))
        {
            return true;
        }

        var remote = context.Connection.RemoteIpAddress;
        if (remote is null)
        {
            return false;
        }

        if (IPAddress.IsLoopback(remote))
        {
            return true;
        }

        var allowed = configuration["Observability:MetricsAllowedNetworks"]
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];
        return allowed.Contains(remote.ToString(), StringComparer.Ordinal);
    }
}
