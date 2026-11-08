using Avila.Platform.Tenancy;

namespace Avila.Modules.Administration.Contracts;

public sealed record CreateCompanyCommand(string Name, string TaxId);

public sealed record CompanyDto(Guid Id, Guid TenantId, string Name, string TaxId);

public sealed record CreatePartyCommand(string Name, string TaxId, bool IsCustomer, bool IsSupplier);

public sealed record PartyDto(Guid Id, Guid TenantId, string Name, string TaxId, bool IsCustomer, bool IsSupplier);

public interface IAdministrationModule
{
    Task<CompanyDto> CreateCompanyAsync(
        IUnitOfWork unitOfWork,
        CreateCompanyCommand command,
        CancellationToken cancellationToken = default);

    Task<PartyDto> CreatePartyAsync(
        IUnitOfWork unitOfWork,
        CreatePartyCommand command,
        CancellationToken cancellationToken = default);
}
