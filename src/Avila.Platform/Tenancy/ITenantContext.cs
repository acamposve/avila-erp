namespace Avila.Platform.Tenancy;

public interface ITenantContext
{
    bool IsResolved { get; }
    Guid TenantId { get; }
    void SetTenant(Guid tenantId);
}

/// <summary>
/// Scoped tenant context. Identity and membership validation populate it once
/// per request; downstream code must never derive tenant from a header or body.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private Guid? _tenantId;

    public bool IsResolved => _tenantId.HasValue;

    public Guid TenantId => _tenantId
        ?? throw new InvalidOperationException("Tenant context has not been resolved.");

    public void SetTenant(Guid tenantId)
    {
        if (_tenantId.HasValue && _tenantId.Value != tenantId)
        {
            throw new InvalidOperationException("Tenant context is already resolved to another tenant.");
        }

        _tenantId = tenantId;
    }
}
