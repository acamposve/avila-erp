using Avila.Modules.Administration.Contracts;
using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;

namespace Avila.Application;

public sealed record ProvisionOrganizationRequest(
    string OrganizationName,
    string CompanyName,
    string TaxId,
    string FiscalAddress,
    string BranchCode,
    string BranchName,
    string? BranchAddress,
    string AdminEmail);

public sealed record ProvisionOrganizationResult(
    Guid TenantId,
    Guid CompanyId,
    Guid BranchId,
    string AdminInvitationToken,
    DateTimeOffset AdminInvitationExpiresAt);

/// <summary>
/// Creates the organization/company/branch in the tenant database and invites
/// the administrator. Platform, tenant and identity stores are separate roles,
/// so this is a staged operation with explicit retry semantics, not one ACID
/// transaction across schemas.
/// </summary>
public sealed class TenantProvisioningService
{
    private readonly IAdministrationModule _administration;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly PlatformTenantProvisioner _platformProvisioner;
    private readonly IdentityService _identityService;
    private readonly TenantSettingsBackfill _settingsBackfill;

    public TenantProvisioningService(
        IAdministrationModule administration,
        IUnitOfWorkFactory unitOfWorkFactory,
        PlatformTenantProvisioner platformProvisioner,
        IdentityService identityService,
        TenantSettingsBackfill settingsBackfill)
    {
        _administration = administration;
        _unitOfWorkFactory = unitOfWorkFactory;
        _platformProvisioner = platformProvisioner;
        _identityService = identityService;
        _settingsBackfill = settingsBackfill;
    }

    public async Task<ProvisionOrganizationResult> ProvisionAsync(
        string tenantConnectionString,
        ProvisionOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FiscalAddress))
        {
            throw new DomainException("Fiscal address is required for a complete provisioning profile.");
        }

        var tenantId = Guid.NewGuid();
        await _platformProvisioner.CreateAsync(tenantId, request.OrganizationName, cancellationToken);

        Guid companyId;
        Guid branchId;

        await using (var unitOfWork = await _unitOfWorkFactory.BeginAsync(
                         tenantConnectionString,
                         tenantId,
                         cancellationToken))
        {
            var company = await _administration.CreateCompanyAsync(
                unitOfWork,
                new CreateCompanyCommand(
                    request.CompanyName,
                    request.TaxId,
                    request.FiscalAddress,
                    FiscalProfileComplete: true),
                cancellationToken);

            var branch = await _administration.CreateBranchAsync(
                unitOfWork,
                new CreateBranchCommand(
                    company.Id,
                    request.BranchCode,
                    request.BranchName,
                    request.BranchAddress),
                cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);
            companyId = company.Id;
            branchId = branch.Id;
        }

        await _settingsBackfill.SeedAsync(
            tenantConnectionString,
            [tenantId],
            "locale",
            "es-VE",
            cancellationToken);

        var invitation = await _identityService.InviteAsync(
            tenantId,
            request.AdminEmail,
            "owner",
            cancellationToken);

        return new ProvisionOrganizationResult(
            tenantId,
            companyId,
            branchId,
            invitation.Token,
            invitation.ExpiresAt);
    }
}
