using Avila.SharedKernel;
using Microsoft.EntityFrameworkCore;

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

        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        await using var context = new PlatformDbContext(options);

        var existing = await context.Tenants
            .SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var normalizedName = name.Trim();
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
        await context.SaveChangesAsync(cancellationToken);
        return tenant;
    }
}
