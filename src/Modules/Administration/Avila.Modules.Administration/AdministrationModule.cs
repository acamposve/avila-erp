using Avila.Modules.Administration.Contracts;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;

namespace Avila.Modules.Administration;

public sealed class AdministrationModule : IAdministrationModule
{
    public async Task<CompanyDto> CreateCompanyAsync(
        IUnitOfWork unitOfWork,
        CreateCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.TaxId))
        {
            throw new DomainException("Company name and tax id are required.");
        }

        await using var context = new AdministrationDbContext(
            unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
        unitOfWork.Enlist(context);

        var company = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            Name = command.Name.Trim(),
            TaxId = command.TaxId.Trim()
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync(cancellationToken);

        return new CompanyDto(company.Id, company.TenantId, company.Name, company.TaxId);
    }

    public async Task<PartyDto> CreatePartyAsync(
        IUnitOfWork unitOfWork,
        CreatePartyCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.TaxId))
        {
            throw new DomainException("Party name and tax id are required.");
        }

        await using var context = new AdministrationDbContext(
            unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
        unitOfWork.Enlist(context);

        var party = new Party
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            Name = command.Name.Trim(),
            TaxId = command.TaxId.Trim(),
            IsCustomer = command.IsCustomer,
            IsSupplier = command.IsSupplier
        };

        context.Parties.Add(party);
        await context.SaveChangesAsync(cancellationToken);

        return new PartyDto(
            party.Id,
            party.TenantId,
            party.Name,
            party.TaxId,
            party.IsCustomer,
            party.IsSupplier);
    }
}
