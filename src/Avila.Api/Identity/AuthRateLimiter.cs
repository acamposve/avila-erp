using System.Collections.Concurrent;

namespace Avila.Api.Identity;

public sealed class AuthRateLimiter
{
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _hits = new();

    public bool IsAllowed(
        string key,
        int limit,
        TimeSpan window,
        out TimeSpan retryAfter)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - window;
        var hits = _hits.GetOrAdd(key, _ => []);
        lock (hits)
        {
            hits.RemoveAll(x => x <= cutoff);
            if (hits.Count >= limit)
            {
                retryAfter = window - (now - hits[0]);
                if (retryAfter < TimeSpan.Zero)
                {
                    retryAfter = TimeSpan.Zero;
                }

                return false;
            }

            hits.Add(now);
        }

        retryAfter = TimeSpan.Zero;
        return true;
    }
}

public sealed class AuthRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthRateLimiter _limiter;
    private readonly ILogger<AuthRateLimitingMiddleware> _logger;

    public AuthRateLimitingMiddleware(
        RequestDelegate next,
        AuthRateLimiter limiter,
        ILogger<AuthRateLimitingMiddleware> logger)
    {
        _next = next;
        _limiter = limiter;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var limit = path.Contains("/login", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/password/forgot", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/mfa/", StringComparison.OrdinalIgnoreCase)
            ? 10
            : 30;
        var key = $"{context.Connection.RemoteIpAddress}:{path}";
        if (!_limiter.IsAllowed(key, limit, TimeSpan.FromMinutes(1), out var retryAfter))
        {
            _logger.LogWarning(
                "Auth rate limit exceeded for {Path}. Correlation {CorrelationId}",
                path,
                context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                error = "RATE_LIMITED",
                retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds)
            });
            return;
        }

        await _next(context);
    }
}
