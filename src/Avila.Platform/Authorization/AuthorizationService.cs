using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Authorization;

public static class Permissions
{
    public const string AdministrationCompanyRead = "administration.company.read";
    public const string AdministrationUserManage = "administration.user.manage";
    public const string AdministrationSettingsManage = "administration.settings.manage";
    public const string AdministrationApprovalManage = "administration.approval.manage";
    public const string SalesQuoteCreate = "sales.quote.create";
    public const string SalesQuoteRead = "sales.quote.read";
    public const string InventoryStockRead = "inventory.stock.read";
    public const string InventoryStockAdjust = "inventory.stock.adjust";
    public const string PlatformSupportAccess = "platform.support.access";
}

public static class RolePermissions
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>> Map =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
        {
            ["member"] =
            [
                Permissions.AdministrationCompanyRead,
                Permissions.SalesQuoteCreate,
                Permissions.SalesQuoteRead,
                Permissions.InventoryStockRead
            ],
            ["admin"] = [.. AllTenantPermissions()],
            ["owner"] = [.. AllTenantPermissions()]
        };

    public static bool Allows(string role, string permission)
        => Map.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    private static IEnumerable<string> AllTenantPermissions() =>
    [
        Permissions.AdministrationCompanyRead,
        Permissions.AdministrationUserManage,
        Permissions.AdministrationSettingsManage,
        Permissions.AdministrationApprovalManage,
        Permissions.SalesQuoteCreate,
        Permissions.SalesQuoteRead,
        Permissions.InventoryStockRead,
        Permissions.InventoryStockAdjust
    ];
}

public sealed record AccessScopeRef(string Type, Guid Id)
{
    public static AccessScopeRef Company(Guid id) => new("company", id);
    public static AccessScopeRef Branch(Guid id) => new("branch", id);
    public static AccessScopeRef Warehouse(Guid id) => new("warehouse", id);
}

public sealed class AuthorizationException : Exception
{
    public AuthorizationException(string permission)
        : base($"Permission denied: {permission}")
    {
        Permission = permission;
    }

    public string Permission { get; }
}

/// <summary>
/// Server-side RBAC. Owner/admin have tenant-wide access; a member needs an
/// explicit company/branch/warehouse scope. There is no client-side trust.
/// </summary>
public sealed class AuthorizationService
{
    private readonly SecurityAuditWriter? _deniedAudit;

    public AuthorizationService(SecurityAuditWriter? deniedAudit = null)
    {
        _deniedAudit = deniedAudit;
    }

    public async Task<bool> IsAllowedAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal principal,
        string permission,
        AccessScopeRef? scope = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (principal.TenantId != unitOfWork.TenantId)
        {
            await RecordDeniedAsync(
                unitOfWork,
                principal,
                permission,
                "cross_tenant",
                scope,
                correlationId,
                cancellationToken);
            return false;
        }

        if (!RolePermissions.Allows(principal.Role, permission))
        {
            await RecordDeniedAsync(
                unitOfWork,
                principal,
                permission,
                "permission",
                scope,
                correlationId,
                cancellationToken);
            return false;
        }

        if (principal.Role is "owner" or "admin")
        {
            return true;
        }

        if (scope is null || !IsValidScopeType(scope.Type))
        {
            await RecordDeniedAsync(
                unitOfWork,
                principal,
                permission,
                "scope",
                scope,
                correlationId,
                cancellationToken);
            return false;
        }

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var allowed = await context.AccessScopes.AnyAsync(
            x => x.TenantId == unitOfWork.TenantId
                 && x.UserId == principal.UserId
                 && x.ScopeType == scope.Type
                 && x.ScopeId == scope.Id,
            cancellationToken);

        if (!allowed)
        {
            await RecordDeniedAsync(
                unitOfWork,
                principal,
                permission,
                "scope",
                scope,
                correlationId,
                cancellationToken);
        }

        return allowed;
    }

    public async Task EnsureAllowedAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal principal,
        string permission,
        AccessScopeRef? scope = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (!await IsAllowedAsync(unitOfWork, principal, permission, scope, correlationId, cancellationToken))
        {
            throw new AuthorizationException(permission);
        }
    }

    private async Task RecordDeniedAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal principal,
        string permission,
        string reason,
        AccessScopeRef? scope,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (_deniedAudit is null)
        {
            return;
        }

        await _deniedAudit.RecordDeniedAsync(
            unitOfWork.TenantId,
            principal.UserId,
            principal.TenantId,
            permission,
            reason,
            scope,
            correlationId,
            cancellationToken);
    }

    public async Task AssignScopeAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        Guid userId,
        AccessScopeRef scope,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidScopeType(scope.Type))
        {
            throw new ArgumentException("Scope type must be company, branch or warehouse.", nameof(scope));
        }

        await EnsureAllowedAsync(
            unitOfWork,
            actor,
            Permissions.AdministrationUserManage,
            cancellationToken: cancellationToken);

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var exists = await context.AccessScopes.AnyAsync(
            x => x.TenantId == unitOfWork.TenantId
                 && x.UserId == userId
                 && x.ScopeType == scope.Type
                 && x.ScopeId == scope.Id,
            cancellationToken);

        if (exists)
        {
            return;
        }

        context.AccessScopes.Add(new AccessScope
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            UserId = userId,
            ScopeType = scope.Type,
            ScopeId = scope.Id
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsValidScopeType(string type)
        => type is "company" or "branch" or "warehouse";
}
