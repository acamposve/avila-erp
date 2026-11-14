using Avila.Platform.Tenancy;
using Avila.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Avila.Platform.Persistence;

/// <summary>
/// Organization provisioning through the dedicated platform role. The app role
/// cannot read or create global tenant records.
/// </summary>
public sealed class PlatformTenantProvisioner
{
    private readonly string _connectionString;

    public PlatformTenantProvisioner(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<TenantRecord> CreateAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Organization name is required.");
        }

        var normalizedName = name.Trim();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(connection)
            .Options;

        await using var context = new PlatformDbContext(options);
        context.Database.UseTransaction(transaction);

        var existing = await context.Tenants
            .SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        if (await context.Tenants.AnyAsync(x => x.Name == normalizedName, cancellationToken))
        {
            throw new DomainException("An organization with this name already exists.");
        }

        var tenant = new TenantRecord
        {
            Id = tenantId,
            Name = normalizedName,
            Status = "active"
        };

        context.Tenants.Add(tenant);
        context.TenantLifecycles.Add(new TenantLifecycle
        {
            TenantId = tenantId,
            Status = "active",
            PlanCode = "trial"
        });

        await TenantSession.SetTenantAsync(connection, transaction, tenantId, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return tenant;
    }
}
