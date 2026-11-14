using Avila.Application;
using Avila.Modules.Administration;
using Avila.Modules.Billing;
using Avila.Modules.Inventory;
using Avila.Modules.Sales;
using Avila.Platform.Idempotency;
using Avila.Platform.Numbering;
using Avila.Platform.Tenancy;
using Npgsql;
using System.Collections.Concurrent;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class NumberingIdempotencyTests
{
    private readonly ProductionFixture _fixture;

    public NumberingIdempotencyTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Numbers_are_gapless_and_isolated_by_company_and_branch()
    {
        var tenant = Guid.NewGuid();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var branch = Guid.NewGuid();

        Assert.Equal("Q-00000001", await ReserveAsync(tenant, DocumentScope.Company(companyA), "quote", "Q"));
        Assert.Equal("Q-00000002", await ReserveAsync(tenant, DocumentScope.Company(companyA), "quote", "Q"));
        Assert.Equal("Q-00000003", await ReserveAsync(tenant, DocumentScope.Company(companyA), "quote", "Q"));
        Assert.Equal("Q-00000001", await ReserveAsync(tenant, DocumentScope.Company(companyB), "quote", "Q"));
        Assert.Equal("B-00000001", await ReserveAsync(tenant, DocumentScope.Branch(branch), "quote", "B"));
    }

    [Fact]
    public async Task Rollback_does_not_consume_a_number()
    {
        var tenant = Guid.NewGuid();
        var company = Guid.NewGuid();

        Assert.Equal("Q-00000001", await ReserveAsync(tenant, DocumentScope.Company(company), "quote", "Q"));

        await using (var unitOfWork = await BeginAsync(tenant))
        {
            Assert.Equal("Q-00000002", await DocumentNumberService.ReserveAsync(
                unitOfWork,
                DocumentScope.Company(company),
                "quote",
                "Q"));
            await unitOfWork.RollbackAsync();
        }

        Assert.Equal("Q-00000002", await ReserveAsync(tenant, DocumentScope.Company(company), "quote", "Q"));
    }

    [Fact]
    public async Task Ten_thousand_concurrent_numbers_are_unique_and_gapless()
    {
        var tenant = Guid.NewGuid();
        var company = Guid.NewGuid();
        const int count = 10_000;
        var connectionString = new NpgsqlConnectionStringBuilder(_fixture.AppConnectionString)
        {
            MaxPoolSize = 100
        }.ConnectionString;
        var numbers = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(
            Enumerable.Range(0, count),
            new ParallelOptions { MaxDegreeOfParallelism = 64 },
            async (_, cancellationToken) =>
            {
                var number = await ReserveAsync(
                    tenant,
                    DocumentScope.Company(company),
                    "quote",
                    "Q",
                    connectionString,
                    cancellationToken);
                numbers.Add(number);
            });

        Assert.Equal(count, numbers.Distinct().Count());
        Assert.Equal("Q-00000001", numbers.Min());
        Assert.Equal("Q-00010000", numbers.Max());
    }

    [Fact]
    public async Task Same_idempotency_key_replays_without_duplicating_rows()
    {
        var tenant = Guid.NewGuid();
        var request = Request("idem-1");
        var service = Flow();

        var first = await service.RunAsync(_fixture.AppConnectionString, tenant, request);
        var second = await service.RunAsync(_fixture.AppConnectionString, tenant, request);

        Assert.Equal(first.CompanyId, second.CompanyId);
        Assert.Equal(first.QuoteId, second.QuoteId);
        Assert.Equal(1, await CountAsync("admin.companies", tenant));
        Assert.Equal(1, await CountAsync("sales.quotes", tenant));
        Assert.Equal(1, await CountAsync("sales.orders", tenant));
        Assert.Equal(1, await CountAsync("billing.invoice_drafts", tenant));
    }

    [Fact]
    public async Task Same_key_with_different_request_is_rejected()
    {
        var tenant = Guid.NewGuid();
        var service = Flow();
        var request = Request("idem-2");

        await service.RunAsync(_fixture.AppConnectionString, tenant, request);

        var exception = await Assert.ThrowsAsync<IdempotencyConflictException>(() => service.RunAsync(
            _fixture.AppConnectionString,
            tenant,
            request with { CompanyName = "Different Company" }));

        Assert.Equal("IDEMPOTENCY_KEY_REUSED", exception.Code);
        Assert.Equal(1, await CountAsync("admin.companies", tenant));
    }

    [Fact]
    public async Task Concurrent_same_key_executes_one_effect()
    {
        var tenant = Guid.NewGuid();
        var request = Request("idem-3");
        var service = Flow();

        var results = await Task.WhenAll(
            service.RunAsync(_fixture.AppConnectionString, tenant, request),
            service.RunAsync(_fixture.AppConnectionString, tenant, request));

        Assert.Equal(results[0].CompanyId, results[1].CompanyId);
        Assert.Equal(1, await CountAsync("admin.companies", tenant));
        Assert.Equal(1, await CountAsync("sales.quotes", tenant));
    }

    [Fact]
    public async Task Quote_and_order_numbers_replace_guid_placeholders()
    {
        var tenant = Guid.NewGuid();
        var result = await Flow().RunAsync(_fixture.AppConnectionString, tenant, Request("numbers"));

        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var quote = new NpgsqlCommand(
            "SELECT number FROM sales.quotes WHERE id = @id",
            connection);
        quote.Parameters.AddWithValue("id", result.QuoteId);
        Assert.Equal("Q-00000001", (string?)await quote.ExecuteScalarAsync());

        await using var order = new NpgsqlCommand(
            "SELECT number FROM sales.orders WHERE id = @id",
            connection);
        order.Parameters.AddWithValue("id", result.OrderId);
        Assert.Equal("O-00000001", (string?)await order.ExecuteScalarAsync());
    }

    private SalesFlowService Flow()
        => new(
            new AdministrationModule(),
            new InventoryModule(),
            new SalesModule(),
            new BillingModule(),
            new TenantUnitOfWorkFactory());

    private static SalesFlowRequest Request(string idempotencyKey)
        => new(
            CompanyName: $"Company {idempotencyKey}",
            CompanyTaxId: $"J-{Guid.NewGuid():N}",
            CustomerName: "Customer",
            CustomerTaxId: $"V-{Guid.NewGuid():N}",
            ItemSku: $"SKU-{Guid.NewGuid():N}",
            ItemName: "Item",
            UnitPrice: 10m,
            TaxRate: 0.16m,
            Quantity: 1m,
            Currency: "VES",
            CompanyFiscalAddress: "Av. Principal, Caracas",
            CompleteFiscalProfile: true,
            IdempotencyKey: idempotencyKey);

    private async Task<string> ReserveAsync(
        Guid tenant,
        DocumentScope scope,
        string documentType,
        string prefix,
        string? connectionString = null,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = await BeginAsync(tenant, connectionString, cancellationToken);
        var number = await DocumentNumberService.ReserveAsync(
            unitOfWork,
            scope,
            documentType,
            prefix,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return number;
    }

    private async Task<IUnitOfWork> BeginAsync(
        Guid tenant,
        string? connectionString = null,
        CancellationToken cancellationToken = default)
        => await new TenantUnitOfWorkFactory().BeginAsync(
            connectionString ?? _fixture.AppConnectionString,
            tenant,
            cancellationToken);

    private async Task<int> CountAsync(string table, Guid tenant)
    {
        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM {table} WHERE tenant_id = @tenant",
            connection);
        command.Parameters.AddWithValue("tenant", tenant);
        return (int)(long)(await command.ExecuteScalarAsync() ?? 0L);
    }
}
