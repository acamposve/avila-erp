using Avila.Spike.Billing;
using Avila.Spike.Tenancy;
using Avila.Spike.Treasury;

namespace Avila.IntegrationTests;

[Collection("postgres")]
public sealed class TransactionRollbackTests
{
    private readonly PostgresFixture _fixture;

    public TransactionRollbackTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task T04_failure_before_commit_rolls_back_both_module_contexts()
    {
        var tenant = Guid.NewGuid();

        await using (var unitOfWork = await TenantUnitOfWork.BeginAsync(
            _fixture.AppConnectionString,
            tenant))
        {
            unitOfWork.Billing.Invoices.Add(new Invoice
            {
                Id = Guid.NewGuid(),
                TenantId = tenant,
                CompanyId = Guid.NewGuid(),
                Number = 1,
                Status = "issued"
            });

            await unitOfWork.SaveBillingAsync();

            unitOfWork.Treasury.Receivables.Add(new Receivable
            {
                Id = Guid.NewGuid(),
                TenantId = tenant,
                CustomerId = Guid.NewGuid(),
                OriginalAmount = 100m,
                AppliedAmount = 0m,
                Currency = "VES",
                Status = "open"
            });

            await unitOfWork.SaveTreasuryAsync();

            // Failure happens before commit: both contexts must be rolled back.
            await unitOfWork.RollbackAsync();
        }

        Assert.Equal(0, await TestDb.CountInvoicesAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(0, await TestDb.CountReceivablesAsync(_fixture.AdminConnectionString, tenant));
    }

    [Fact]
    public async Task T04b_commit_persists_both_module_contexts()
    {
        var tenant = Guid.NewGuid();

        await using (var unitOfWork = await TenantUnitOfWork.BeginAsync(
            _fixture.AppConnectionString,
            tenant))
        {
            unitOfWork.Billing.Invoices.Add(new Invoice
            {
                Id = Guid.NewGuid(),
                TenantId = tenant,
                CompanyId = Guid.NewGuid(),
                Number = 1,
                Status = "issued"
            });

            await unitOfWork.SaveBillingAsync();

            unitOfWork.Treasury.Receivables.Add(new Receivable
            {
                Id = Guid.NewGuid(),
                TenantId = tenant,
                CustomerId = Guid.NewGuid(),
                OriginalAmount = 100m,
                AppliedAmount = 0m,
                Currency = "VES",
                Status = "open"
            });

            await unitOfWork.SaveTreasuryAsync();
            await unitOfWork.CommitAsync();
        }

        Assert.Equal(1, await TestDb.CountInvoicesAsync(_fixture.AdminConnectionString, tenant));
        Assert.Equal(1, await TestDb.CountReceivablesAsync(_fixture.AdminConnectionString, tenant));
    }
}
