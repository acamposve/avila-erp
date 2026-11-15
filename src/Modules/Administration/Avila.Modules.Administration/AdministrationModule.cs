using Avila.Modules.Administration.Contracts;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Avila.Modules.Administration;

public sealed class AdministrationNotFoundException : Exception
{
    public AdministrationNotFoundException(string message) : base(message)
    {
    }
}

public sealed class AdministrationVersionConflictException : Exception
{
    public AdministrationVersionConflictException(string message) : base(message)
    {
    }
}

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
            company.FiscalProfileComplete,
            company.Version);
    }

    public async Task<CompanyDto> UpdateCompanyAsync(
        IUnitOfWork unitOfWork,
        UpdateCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.TaxId))
        {
            throw new DomainException("Company name and tax id are required.");
        }

        await using var context = new AdministrationDbContext(
            unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
        unitOfWork.Enlist(context);

        var company = await context.Companies
            .SingleOrDefaultAsync(
                x => x.Id == command.Id && x.TenantId == unitOfWork.TenantId,
                cancellationToken);
        if (company is null)
        {
            throw new AdministrationNotFoundException($"Company {command.Id} was not found.");
        }

        if (company.Version != command.Version)
        {
            throw new AdministrationVersionConflictException(
                $"Company {command.Id} was modified by another request.");
        }

        var taxId = command.TaxId.Trim();
        var duplicate = await context.Companies.AnyAsync(
            x => x.TenantId == unitOfWork.TenantId
                 && x.Id != company.Id
                 && x.TaxId == taxId,
            cancellationToken);
        if (duplicate)
        {
            throw new DomainException("Company tax id already exists.");
        }

        if (command.FiscalProfileComplete && string.IsNullOrWhiteSpace(command.FiscalAddress))
        {
            throw new DomainException("A complete fiscal profile requires a fiscal address.");
        }

        company.Name = command.Name.Trim();
        company.TaxId = taxId;
        company.FiscalAddress = command.FiscalAddress?.Trim();
        company.FiscalProfileComplete = command.FiscalProfileComplete;
        company.Version++;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AdministrationVersionConflictException(
                $"Company {command.Id} was modified by another request.");
        }

        return new CompanyDto(
            company.Id,
            company.TenantId,
            company.Name,
            company.TaxId,
            company.FiscalAddress,
            company.FiscalProfileComplete,
            company.Version);
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
            branch.IsActive,
            branch.Version);
    }

    public async Task<CompanyDto?> GetCompanyAsync(
        IUnitOfWork unitOfWork,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = new AdministrationDbContext(
            unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
        unitOfWork.Enlist(context);
        return await context.Companies
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == unitOfWork.TenantId)
            .Select(x => new CompanyDto(
                x.Id,
                x.TenantId,
                x.Name,
                x.TaxId,
                x.FiscalAddress,
                x.FiscalProfileComplete,
                x.Version))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<BranchDto?> GetBranchAsync(
        IUnitOfWork unitOfWork,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = new AdministrationDbContext(
            unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
        unitOfWork.Enlist(context);
        return await context.Branches
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == unitOfWork.TenantId)
            .Select(x => new BranchDto(
                x.Id,
                x.TenantId,
                x.CompanyId,
                x.Code,
                x.Name,
                x.Address,
                x.IsActive,
                x.Version))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<BranchDto> UpdateBranchAsync(
        IUnitOfWork unitOfWork,
        UpdateBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.Name))
        {
            throw new DomainException("Branch code and name are required.");
        }

        await using var context = new AdministrationDbContext(
            unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
        unitOfWork.Enlist(context);

        var branch = await context.Branches
            .SingleOrDefaultAsync(
                x => x.Id == command.Id && x.TenantId == unitOfWork.TenantId,
                cancellationToken);
        if (branch is null)
        {
            throw new AdministrationNotFoundException($"Branch {command.Id} was not found.");
        }

        if (branch.Version != command.Version)
        {
            throw new AdministrationVersionConflictException(
                $"Branch {command.Id} was modified by another request.");
        }

        var code = command.Code.Trim();
        var duplicate = await context.Branches.AnyAsync(
            x => x.TenantId == unitOfWork.TenantId
                 && x.CompanyId == branch.CompanyId
                 && x.Id != branch.Id
                 && x.Code == code,
            cancellationToken);
        if (duplicate)
        {
            throw new DomainException("Branch code already exists for this company.");
        }

        branch.Code = code;
        branch.Name = command.Name.Trim();
        branch.Address = command.Address?.Trim();
        branch.IsActive = command.IsActive;
        branch.Version++;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AdministrationVersionConflictException(
                $"Branch {command.Id} was modified by another request.");
        }

        return new BranchDto(
            branch.Id,
            branch.TenantId,
            branch.CompanyId,
            branch.Code,
            branch.Name,
            branch.Address,
            branch.IsActive,
            branch.Version);
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

    public async Task<TenantExportDto> ExportAsync(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        await using var context = new AdministrationDbContext(
            unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
        unitOfWork.Enlist(context);

        var companies = await context.Companies
            .OrderBy(x => x.Name)
            .Select(x => new CompanyExportDto(
                x.Id,
                x.Name,
                x.TaxId,
                x.FiscalAddress,
                x.FiscalProfileComplete))
            .ToListAsync(cancellationToken);

        var branches = await context.Branches
            .OrderBy(x => x.Code)
            .Select(x => new BranchExportDto(
                x.Id,
                x.CompanyId,
                x.Code,
                x.Name,
                x.IsActive))
            .ToListAsync(cancellationToken);

        var parties = await context.Parties
            .OrderBy(x => x.Name)
            .Select(x => new PartyExportDto(
                x.Id,
                x.Name,
                x.TaxId,
                x.IsCustomer,
                x.IsSupplier))
            .ToListAsync(cancellationToken);

        return new TenantExportDto(unitOfWork.TenantId, companies, branches, parties);
    }
}
