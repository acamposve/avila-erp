using Avila.Platform.Tenancy;

namespace Avila.Modules.Administration.Contracts;

public sealed record CreateCompanyCommand(
    string Name,
    string TaxId,
    string? FiscalAddress = null,
    bool FiscalProfileComplete = false);

public sealed record CompanyDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string TaxId,
    string? FiscalAddress,
    bool FiscalProfileComplete);

public sealed record CreateBranchCommand(
    Guid CompanyId,
    string Code,
    string Name,
    string? Address = null);

public sealed record BranchDto(
    Guid Id,
    Guid TenantId,
    Guid CompanyId,
    string Code,
    string Name,
    string? Address,
    bool IsActive);

public sealed record CreatePartyCommand(string Name, string TaxId, bool IsCustomer, bool IsSupplier);

public sealed record PartyDto(Guid Id, Guid TenantId, string Name, string TaxId, bool IsCustomer, bool IsSupplier);

public interface IAdministrationModule
{
    Task<CompanyDto> CreateCompanyAsync(
        IUnitOfWork unitOfWork,
        CreateCompanyCommand command,
        CancellationToken cancellationToken = default);

    Task<BranchDto> CreateBranchAsync(
        IUnitOfWork unitOfWork,
        CreateBranchCommand command,
        CancellationToken cancellationToken = default);

    Task<PartyDto> CreatePartyAsync(
        IUnitOfWork unitOfWork,
        CreatePartyCommand command,
        CancellationToken cancellationToken = default);
}
