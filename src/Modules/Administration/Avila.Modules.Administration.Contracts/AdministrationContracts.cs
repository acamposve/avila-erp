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
    bool FiscalProfileComplete,
    int Version = 1);

public sealed record UpdateCompanyCommand(
    Guid Id,
    string Name,
    string TaxId,
    string? FiscalAddress,
    bool FiscalProfileComplete,
    int Version);

public sealed record CreateBranchCommand(
    Guid CompanyId,
    string Code,
    string Name,
    string? Address = null);

public sealed record UpdateBranchCommand(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    bool IsActive,
    int Version);

public sealed record BranchDto(
    Guid Id,
    Guid TenantId,
    Guid CompanyId,
    string Code,
    string Name,
    string? Address,
    bool IsActive,
    int Version = 1);

public sealed record CompanyExportDto(
    Guid Id,
    string Name,
    string TaxId,
    string? FiscalAddress,
    bool FiscalProfileComplete);

public sealed record BranchExportDto(Guid Id, Guid CompanyId, string Code, string Name, bool IsActive);

public sealed record PartyExportDto(Guid Id, string Name, string TaxId, bool IsCustomer, bool IsSupplier);

public sealed record TenantExportDto(
    Guid TenantId,
    IReadOnlyList<CompanyExportDto> Companies,
    IReadOnlyList<BranchExportDto> Branches,
    IReadOnlyList<PartyExportDto> Parties);

public sealed record CreatePartyCommand(string Name, string TaxId, bool IsCustomer, bool IsSupplier);

public sealed record PartyDto(Guid Id, Guid TenantId, string Name, string TaxId, bool IsCustomer, bool IsSupplier);

public interface IAdministrationModule
{
    Task<CompanyDto> CreateCompanyAsync(
        IUnitOfWork unitOfWork,
        CreateCompanyCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyDto> UpdateCompanyAsync(
        IUnitOfWork unitOfWork,
        UpdateCompanyCommand command,
        CancellationToken cancellationToken = default);

    Task<CompanyDto?> GetCompanyAsync(
        IUnitOfWork unitOfWork,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BranchDto> CreateBranchAsync(
        IUnitOfWork unitOfWork,
        CreateBranchCommand command,
        CancellationToken cancellationToken = default);

    Task<BranchDto> UpdateBranchAsync(
        IUnitOfWork unitOfWork,
        UpdateBranchCommand command,
        CancellationToken cancellationToken = default);

    Task<BranchDto?> GetBranchAsync(
        IUnitOfWork unitOfWork,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PartyDto> CreatePartyAsync(
        IUnitOfWork unitOfWork,
        CreatePartyCommand command,
        CancellationToken cancellationToken = default);

    Task<TenantExportDto> ExportAsync(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default);
}
