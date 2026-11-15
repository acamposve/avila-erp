using System.Runtime.CompilerServices;
using System.Text.Json;
using Avila.Modules.Administration.Contracts;
using Avila.Platform.Exporting;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.Modules.Administration;

/// <summary>
/// Administration owns its export schema. The orchestrator only sees
/// ITenantExportContributor.
/// </summary>
public sealed class AdministrationExportContributor : ITenantExportContributor, ITenantExportStreamContributor
{
    private readonly IAdministrationModule _module;

    public AdministrationExportContributor(IAdministrationModule module)
    {
        _module = module;
    }

    public string ModuleKey => "administration";
    public int Order => 10;
    public int SchemaVersion => 1;

    public async Task<TenantExportSection> ExportAsync(
        IUnitOfWork unitOfWork,
        TenantExportContext context,
        CancellationToken cancellationToken = default)
    {
        var data = await _module.ExportAsync(unitOfWork, cancellationToken);
        var records = new List<TenantExportRecord>();

        records.AddRange(data.Companies.Select(company => new TenantExportRecord(
            "company",
            company.Id.ToString("D"),
            JsonSerializer.SerializeToElement(new
            {
                name = company.Name,
                tax_id = company.TaxId,
                fiscal_address = company.FiscalAddress,
                fiscal_profile_complete = company.FiscalProfileComplete
            }))));

        records.AddRange(data.Branches.Select(branch => new TenantExportRecord(
            "branch",
            branch.Id.ToString("D"),
            JsonSerializer.SerializeToElement(new
            {
                company_id = branch.CompanyId,
                code = branch.Code,
                name = branch.Name,
                is_active = branch.IsActive
            }))));

        records.AddRange(data.Parties.Select(party => new TenantExportRecord(
            "party",
            party.Id.ToString("D"),
            JsonSerializer.SerializeToElement(new
            {
                name = party.Name,
                tax_id = party.TaxId,
                is_customer = party.IsCustomer,
                is_supplier = party.IsSupplier
            }))));

        return new TenantExportSection(ModuleKey, SchemaVersion, records);
    }

    public async IAsyncEnumerable<TenantExportRecord> StreamAsync(
        IUnitOfWork unitOfWork,
        TenantExportContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var db = new AdministrationDbContext(
            unitOfWork.OptionsFor<AdministrationDbContext>("admin"));
        unitOfWork.Enlist(db);

        await foreach (var company in db.Companies
                           .AsNoTracking()
                           .OrderBy(x => x.Id)
                           .AsAsyncEnumerable()
                           .WithCancellation(cancellationToken))
        {
            yield return new TenantExportRecord(
                "company",
                company.Id.ToString("D"),
                JsonSerializer.SerializeToElement(new
                {
                    name = company.Name,
                    tax_id = company.TaxId,
                    fiscal_address = company.FiscalAddress,
                    fiscal_profile_complete = company.FiscalProfileComplete
                }));
        }

        await foreach (var branch in db.Branches
                           .AsNoTracking()
                           .OrderBy(x => x.Id)
                           .AsAsyncEnumerable()
                           .WithCancellation(cancellationToken))
        {
            yield return new TenantExportRecord(
                "branch",
                branch.Id.ToString("D"),
                JsonSerializer.SerializeToElement(new
                {
                    company_id = branch.CompanyId,
                    code = branch.Code,
                    name = branch.Name,
                    is_active = branch.IsActive
                }));
        }

        await foreach (var party in db.Parties
                           .AsNoTracking()
                           .OrderBy(x => x.Id)
                           .AsAsyncEnumerable()
                           .WithCancellation(cancellationToken))
        {
            yield return new TenantExportRecord(
                "party",
                party.Id.ToString("D"),
                JsonSerializer.SerializeToElement(new
                {
                    name = party.Name,
                    tax_id = party.TaxId,
                    is_customer = party.IsCustomer,
                    is_supplier = party.IsSupplier
                }));
        }
    }
}
