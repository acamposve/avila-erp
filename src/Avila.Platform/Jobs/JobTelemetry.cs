using System.Diagnostics;

namespace Avila.Platform.Jobs;

/// <summary>
/// Job trace instrumentation. Only job name and correlation ID are tagged;
/// tenant/user IDs and payloads must never be attached to spans.
/// </summary>
public static class JobTelemetry
{
    public const string SourceName = "Avila.Jobs";
    public static readonly ActivitySource Source = new(SourceName, "1.0.0");

    public static Activity? Start(string jobName, string? correlationId = null)
    {
        var activity = Activity.Current is null
            ? Source.StartActivity(jobName, ActivityKind.Internal)
            : Source.StartActivity(jobName, ActivityKind.Internal, Activity.Current.Context);
        activity?.SetTag("job.name", jobName);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            activity?.SetTag("correlation.id", correlationId);
        }

        return activity;
    }
}
