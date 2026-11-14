using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Avila.Api.Observability;

public sealed class MetricsRegistry
{
    private long _requests;
    private long _errors;
    private long _durationTicks;
    private readonly ConcurrentDictionary<int, long> _byStatusCode = new();

    public void Record(int statusCode, long elapsedMilliseconds)
    {
        Interlocked.Increment(ref _requests);
        Interlocked.Add(ref _durationTicks, elapsedMilliseconds);
        if (statusCode >= 500)
        {
            Interlocked.Increment(ref _errors);
        }

        _byStatusCode.AddOrUpdate(statusCode, 1, (_, current) => current + 1);
    }

    public string Render()
    {
        var requests = Interlocked.Read(ref _requests);
        var errors = Interlocked.Read(ref _errors);
        var average = requests == 0 ? 0 : Interlocked.Read(ref _durationTicks) / (double)requests;

        var builder = new StringBuilder();
        builder.AppendLine("# TYPE avila_http_requests_total counter");
        builder.AppendLine($"avila_http_requests_total {requests.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine("# TYPE avila_http_errors_total counter");
        builder.AppendLine($"avila_http_errors_total {errors.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine("# TYPE avila_http_request_duration_ms gauge");
        builder.AppendLine($"avila_http_request_duration_ms {average.ToString("F2", CultureInfo.InvariantCulture)}");
        builder.AppendLine("# TYPE avila_http_responses_total counter");
        foreach (var status in _byStatusCode.OrderBy(x => x.Key))
        {
            builder.AppendLine(
                $"avila_http_responses_total{{status=\"{status.Key.ToString(CultureInfo.InvariantCulture)}\"}} {status.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        return builder.ToString();
    }
}

/// <summary>
/// Correlation id + minimal request telemetry. Never logs query strings,
/// headers, bodies or tenant data.
/// </summary>
public sealed class RequestObservabilityMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestObservabilityMiddleware> _logger;

    public RequestObservabilityMiddleware(
        RequestDelegate next,
        ILogger<RequestObservabilityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, MetricsRegistry metrics)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName].FirstOrDefault());
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        var stopwatch = Stopwatch.StartNew();
        var statusCode = 500;
        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "Unhandled {ExceptionType}. Correlation {CorrelationId}",
                exception.GetType().Name,
                correlationId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            metrics.Record(statusCode, stopwatch.ElapsedMilliseconds);
            _logger.LogInformation(
                "HTTP {Method} {Path} -> {StatusCode} in {ElapsedMs} ms. Correlation {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }

    private static string ResolveCorrelationId(string? supplied)
    {
        if (!string.IsNullOrWhiteSpace(supplied)
            && supplied.Length <= 64
            && supplied.All(character => char.IsLetterOrDigit(character) || character is '-' or '_'))
        {
            return supplied;
        }

        return Guid.NewGuid().ToString("N");
    }
}
