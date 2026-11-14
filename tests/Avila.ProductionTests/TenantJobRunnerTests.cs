using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Platform.Jobs;
using Avila.Platform.Tenancy;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class TenantJobRunnerTests
{
    private readonly ProductionFixture _fixture;
    private readonly TenantUnitOfWorkFactory _factory = new();

    public TenantJobRunnerTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Empty_tenant_and_null_job_are_rejected_at_boundary()
    {
        var runner = new TenantJobRunner(_factory, _fixture.AppConnectionString);

        await Assert.ThrowsAsync<ArgumentException>(
            () => runner.RunAsync(Guid.Empty, new NoOpJob()));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => runner.RunAsync(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task Ambient_context_is_cleared_before_job_and_after_completion()
    {
        var tenant = Guid.NewGuid();
        var observedAmbient = new List<Guid?>();
        TenantExecutionContext.Set(Guid.NewGuid());

        var runner = new TenantJobRunner(_factory, _fixture.AppConnectionString);
        await runner.RunAsync(tenant, new AmbientProbeJob(observedAmbient));

        Assert.NotEmpty(observedAmbient);
        Assert.All(observedAmbient, value => Assert.Null(value));
    }

    [Fact]
    public async Task Parallel_interleaved_jobs_keep_tenant_scoped_context()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var runner = new TenantJobRunner(_factory, _fixture.AppConnectionString);

        var jobs = Enumerable.Range(0, 12)
            .SelectMany(index => new[]
            {
                runner.RunAsync(tenantA, new DelayedCompanyJob(tenantA, $"A-{index}", index % 3)),
                runner.RunAsync(tenantB, new DelayedCompanyJob(tenantB, $"B-{index}", (index + 1) % 3))
            });

        await Task.WhenAll(jobs);

        Assert.Equal(12, await CountAsync("admin.companies", tenantA));
        Assert.Equal(12, await CountAsync("admin.companies", tenantB));
    }

    [Fact]
    public async Task Failed_job_rolls_back_and_clears_ambient_context()
    {
        var tenant = Guid.NewGuid();
        var observedAmbient = new List<Guid?>();
        TenantExecutionContext.Set(Guid.NewGuid());
        var runner = new TenantJobRunner(_factory, _fixture.AppConnectionString);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.RunAsync(tenant, new FailingCompanyJob(observedAmbient)));

        Assert.Equal(0, await CountAsync("admin.companies", tenant));
        Assert.Single(observedAmbient);
        Assert.Null(observedAmbient[0]);
    }

    private async Task<long> CountAsync(string table, Guid tenant)
    {
        await using var connection = new Npgsql.NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            $"SELECT COUNT(*) FROM {table} WHERE tenant_id = @tenant",
            connection);
        command.Parameters.AddWithValue("tenant", tenant);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private sealed class NoOpJob : ITenantJob
    {
        public Task ExecuteAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class AmbientProbeJob : ITenantJob
    {
        private readonly List<Guid?> _observed;

        public AmbientProbeJob(List<Guid?> observed)
        {
            _observed = observed;
        }

        public Task ExecuteAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
        {
            _observed.Add(TenantExecutionContext.TenantId);
            return Task.CompletedTask;
        }
    }

    private sealed class DelayedCompanyJob : ITenantJob
    {
        private readonly Guid _expectedTenant;
        private readonly string _name;
        private readonly int _delayMs;

        public DelayedCompanyJob(Guid expectedTenant, string name, int delayMs)
        {
            _expectedTenant = expectedTenant;
            _name = name;
            _delayMs = delayMs;
        }

        public async Task ExecuteAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delayMs, cancellationToken);
            Assert.Equal(_expectedTenant, unitOfWork.TenantId);
            Assert.Null(TenantExecutionContext.TenantId);

            await new AdministrationModule().CreateCompanyAsync(
                unitOfWork,
                new CreateCompanyCommand(
                    _name,
                    $"J-{_expectedTenant:N}-{_name}",
                    "Av. Principal, Caracas",
                    FiscalProfileComplete: true),
                cancellationToken);

            await Task.Delay(_delayMs, cancellationToken);
            Assert.Equal(_expectedTenant, unitOfWork.TenantId);
        }
    }

    private sealed class FailingCompanyJob : ITenantJob
    {
        private readonly List<Guid?> _observed;

        public FailingCompanyJob(List<Guid?> observed)
        {
            _observed = observed;
        }

        public async Task ExecuteAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
        {
            _observed.Add(TenantExecutionContext.TenantId);
            await new AdministrationModule().CreateCompanyAsync(
                unitOfWork,
                new CreateCompanyCommand(
                    "Rolled back job",
                    $"J-{Guid.NewGuid():N}",
                    "Av. Principal, Caracas",
                    FiscalProfileComplete: true),
                cancellationToken);
            throw new InvalidOperationException("Injected job failure.");
        }
    }
}
