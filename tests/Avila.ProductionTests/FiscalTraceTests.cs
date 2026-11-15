using System.Diagnostics;
using Avila.Platform.Authorization;
using Avila.Platform.Fiscal;
using Avila.Platform.Identity;
using Avila.Platform.Jobs;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class FiscalTraceTests
{
    private readonly ProductionFixture _fixture;

    public FiscalTraceTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Fiscal_rule_versions_are_immutable_and_snapshot_retains_version()
    {
        var tenant = Guid.NewGuid();
        var actor = new SessionPrincipal(Guid.NewGuid(), tenant, "fiscal@example.test", "owner");
        var service = new FiscalVersionService();
        await using var unitOfWork = await new TenantUnitOfWorkFactory()
            .BeginAsync(_fixture.AppConnectionString, tenant);

        var rule = await service.PublishRuleAsync(
            unitOfWork,
            actor,
            "invoice.tax",
            """{"rate":"synthetic-0.16"}""",
            "synthetic-test-source",
            DateTimeOffset.UtcNow.AddHours(-1));
        Assert.Equal(1, rule.VersionNumber);

        var effective = await service.GetEffectiveAsync(unitOfWork, "invoice.tax");
        Assert.NotNull(effective);
        Assert.Equal(rule.Id, effective!.Id);

        var documentId = Guid.NewGuid();
        var snapshot = await service.CaptureSnapshotAsync(
            unitOfWork,
            actor,
            "invoice",
            documentId,
            "invoice.tax");
        Assert.Equal(rule.VersionNumber, snapshot.RuleVersion);
        Assert.Equal(rule.Id, snapshot.FiscalRuleVersionId);

        var stored = await service.GetSnapshotAsync(unitOfWork, "invoice", documentId, "invoice.tax");
        Assert.NotNull(stored);
        Assert.Equal(rule.Id, stored!.FiscalRuleVersionId);

        await Assert.ThrowsAsync<DomainException>(() => service.PublishRuleAsync(
            unitOfWork,
            actor,
            "invoice.tax",
            """{"rate":"synthetic-0.20"}""",
            "synthetic-test-source",
            DateTimeOffset.UtcNow));

        await unitOfWork.CommitAsync();
    }

    [Fact]
    public async Task Job_span_has_parent_trace_and_no_tenant_or_user_tags()
    {
        var tenant = Guid.NewGuid();
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == JobTelemetry.SourceName || source.Name == "Avila.Tests.Parent",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        using var parentSource = new ActivitySource("Avila.Tests.Parent");
        using var parent = parentSource.StartActivity("http-request");
        Assert.NotNull(parent);

        var runner = new TenantJobRunner(
            new TenantUnitOfWorkFactory(),
            _fixture.AppConnectionString);
        await runner.RunAsync(tenant, new NoopJob());

        var job = Assert.Single(activities, x => x.OperationName == nameof(NoopJob));
        Assert.Equal(parent!.TraceId, job.TraceId);
        Assert.Equal("NoopJob", job.GetTagItem("job.name"));
        Assert.DoesNotContain(job.TagObjects, tag =>
            tag.Key.Contains("tenant", StringComparison.OrdinalIgnoreCase)
            || tag.Key.Contains("user", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class NoopJob : ITenantJob
    {
        public Task ExecuteAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
