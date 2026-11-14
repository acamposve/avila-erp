using Avila.Modules.Administration.Contracts;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;
using Microsoft.EntityFrameworkCore;

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
            TaxId = command.TaxId.Trim(),
            FiscalAddress = command.FiscalAddress?.Trim(),
            FiscalProfileComplete = command.FiscalProfileComplete
        };

        if (company.FiscalProfileComplete && string.IsNullOrWhiteSpace(company.FiscalAddress))
        {
            throw new DomainException("A complete fiscal profile requires a fiscal address.");
        }

        context.Companies.Add(company);
        await context.SaveChangesAsync(cancellationToken);

        return new CompanyDto(
            company.Id,
            company.TenantId,
            company.Name,
            company.TaxId,
            company.FiscalAddress,
            company.FiscalProfileComplete);
    }

    public async Task<BranchDto> CreateBranchAsync(
        IUnitOfWork unitOfWork,
        CreateBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.Name))
        {
            throw new DomainException("Branch code and name are required.");
        }

        await using var context = new AdministrationDbContext(
            unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
        unitOfWork.Enlist(context);

        var companyExists = await context.Companies.AnyAsync(
            x => x.Id == command.CompanyId && x.TenantId == unitOfWork.TenantId,
            cancellationToken);

        if (!companyExists)
        {
            throw new DomainException($"Company {command.CompanyId} was not found.");
        }

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            CompanyId = command.CompanyId,
            Code = command.Code.Trim(),
            Name = command.Name.Trim(),
            Address = command.Address?.Trim()
        };

        context.Branches.Add(branch);
        await context.SaveChangesAsync(cancellationToken);

        return new BranchDto(
            branch.Id,
            branch.TenantId,
            branch.CompanyId,
            branch.Code,
            branch.Name,
            branch.Address,
            branch.IsActive);
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
