using Avila.Spike.Billing;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class NumberingTests
{
    private readonly PostgresFixture _fixture;

    public NumberingTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T10_company_and_branch_scopes_have_independent_sequences()
    {
        var tenant = Guid.NewGuid();
        var company = Guid.NewGuid();
        var branch = Guid.NewGuid();

        var companyFirst = await DocumentNumberService.NextAsync(
            _fixture.AppConnectionString,
            tenant,
            company,
            "company",
            company,
            "invoice",
            "INV",
            2026);

        var companySecond = await DocumentNumberService.NextAsync(
            _fixture.AppConnectionString,
            tenant,
            company,
            "company",
            company,
            "invoice",
            "INV",
            2026);

        var branchFirst = await DocumentNumberService.NextAsync(
            _fixture.AppConnectionString,
            tenant,
            company,
            "branch",
            branch,
            "invoice",
            "INV",
            2026);

        Assert.Equal(1L, companyFirst);
        Assert.Equal(2L, companySecond);
        Assert.Equal(1L, branchFirst);
    }

    [Fact]
    public async Task T11_concurrent_numbering_is_unique_and_gapless()
    {
        var tenant = Guid.NewGuid();
        var company = Guid.NewGuid();
        const int count = 200;

        var tasks = Enumerable.Range(0, count)
            .Select(_ => DocumentNumberService.NextAsync(
                _fixture.AppConnectionString,
                tenant,
                company,
                "company",
                company,
                "invoice",
                "INV",
                2026))
            .ToArray();

        var values = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(90));
        var ordered = values.OrderBy(value => value).ToArray();

        Assert.Equal(count, values.Distinct().Count());
        Assert.Equal(1L, ordered.First());
        Assert.Equal((long)count, ordered.Last());
        Assert.Equal(count, ordered.Length);
    }

    [Fact]
    public async Task T11b_10000_concurrent_numbers_are_unique_and_gapless()
    {
        var tenant = Guid.NewGuid();
        var company = Guid.NewGuid();
        const int count = 10_000;
        using var throttle = new SemaphoreSlim(32);

        var tasks = Enumerable.Range(0, count)
            .Select(async _ =>
            {
                await throttle.WaitAsync();
                try
                {
                    return await DocumentNumberService.NextAsync(
                        _fixture.AppConnectionString,
                        tenant,
                        company,
                        "company",
                        company,
                        "invoice",
                        "INV",
                        2026);
                }
                finally
                {
                    throttle.Release();
                }
            })
            .ToArray();

        var values = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(300));
        var ordered = values.OrderBy(value => value).ToArray();

        Assert.Equal(count, values.Distinct().Count());
        Assert.Equal(1L, ordered.First());
        Assert.Equal((long)count, ordered.Last());
    }
}
