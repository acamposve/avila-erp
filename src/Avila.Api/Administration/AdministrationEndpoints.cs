using Avila.Modules.Administration;
using Avila.Modules.Administration.Contracts;
using Avila.Platform.Authorization;
using Avila.Platform.Exporting;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Avila.Api.Administration;

public sealed record CreateCompanyRequest(
    string Name,
    string TaxId,
    string? FiscalAddress,
    bool FiscalProfileComplete);

public sealed record UpdateCompanyRequest(
    string Name,
    string TaxId,
    string? FiscalAddress,
    bool FiscalProfileComplete,
    int Version);

public sealed record CreateBranchRequest(
    Guid CompanyId,
    string Code,
    string Name,
    string? Address);

public sealed record UpdateBranchRequest(
    string Code,
    string Name,
    string? Address,
    bool IsActive,
    int Version);

public sealed record CreatePartyRequest(
    string Name,
    string TaxId,
    bool IsCustomer,
    bool IsSupplier);

public static class AdministrationEndpoints
{
    public static IEndpointRouteBuilder MapAdministrationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/administration/companies", CreateCompanyAsync);
        app.MapPatch("/api/v1/administration/companies/{companyId:guid}", UpdateCompanyAsync);
        app.MapPost("/api/v1/administration/branches", CreateBranchAsync);
        app.MapPatch("/api/v1/administration/branches/{branchId:guid}", UpdateBranchAsync);
        app.MapPost("/api/v1/administration/parties", CreatePartyAsync);
        return app;
    }

    private static async Task<IResult> CreateCompanyAsync(
        CreateCompanyRequest request,
        HttpContext context,
        IAdministrationModule module,
        AuthorizationService authorization,
        ExportAuditWriter auditWriter,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(
                context, authorization, Permissions.AdministrationCompanyManage, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var principal = Principal(context);
        var unitOfWork = UnitOfWork(context);
        try
        {
            var company = await module.CreateCompanyAsync(
                unitOfWork,
                new CreateCompanyCommand(
                    request.Name,
                    request.TaxId,
                    request.FiscalAddress,
                    request.FiscalProfileComplete),
                cancellationToken);
            await new AuditService().RecordAsync(
                unitOfWork,
                principal,
                "administration.company.created",
                "company",
                company.Id,
                before: null,
                after: company,
                context.TraceIdentifier,
                cancellationToken);
            return Results.Created($"/api/v1/administration/companies/{company.Id}", company);
        }
        catch (DomainException exception)
        {
            return Results.BadRequest(new { error = "VALIDATION", message = exception.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "CONFLICT", message = "Company could not be created." });
        }
    }

    private static async Task<IResult> UpdateCompanyAsync(
        Guid companyId,
        UpdateCompanyRequest request,
        HttpContext context,
        IAdministrationModule module,
        AuthorizationService authorization,
        ExportAuditWriter auditWriter,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(
                context, authorization, Permissions.AdministrationCompanyManage, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var principal = Principal(context);
        var unitOfWork = UnitOfWork(context);
        var before = await module.GetCompanyAsync(unitOfWork, companyId, cancellationToken);
        if (before is null)
        {
            await AuditNotFoundAsync(
                auditWriter, unitOfWork, principal, "company", companyId, context.TraceIdentifier, cancellationToken);
            return Results.NotFound(new { error = "NOT_FOUND" });
        }

        try
        {
            var after = await module.UpdateCompanyAsync(
                unitOfWork,
                new UpdateCompanyCommand(
                    companyId,
                    request.Name,
                    request.TaxId,
                    request.FiscalAddress,
                    request.FiscalProfileComplete,
                    request.Version),
                cancellationToken);
            await new AuditService().RecordAsync(
                unitOfWork,
                principal,
                "administration.company.updated",
                "company",
                companyId,
                before,
                after,
                context.TraceIdentifier,
                cancellationToken);
            return Results.Ok(after);
        }
        catch (AdministrationNotFoundException)
        {
            await AuditNotFoundAsync(
                auditWriter, unitOfWork, principal, "company", companyId, context.TraceIdentifier, cancellationToken);
            return Results.NotFound(new { error = "NOT_FOUND" });
        }
        catch (AdministrationVersionConflictException)
        {
            return Results.Conflict(new { error = "VERSION_CONFLICT" });
        }
        catch (DomainException exception)
        {
            return Results.BadRequest(new { error = "VALIDATION", message = exception.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "CONFLICT" });
        }
    }

    private static async Task<IResult> CreateBranchAsync(
        CreateBranchRequest request,
        HttpContext context,
        IAdministrationModule module,
        AuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(
                context, authorization, Permissions.AdministrationBranchManage, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var principal = Principal(context);
        var unitOfWork = UnitOfWork(context);
        try
        {
            var branch = await module.CreateBranchAsync(
                unitOfWork,
                new CreateBranchCommand(request.CompanyId, request.Code, request.Name, request.Address),
                cancellationToken);
            await new AuditService().RecordAsync(
                unitOfWork,
                principal,
                "administration.branch.created",
                "branch",
                branch.Id,
                before: null,
                after: branch,
                context.TraceIdentifier,
                cancellationToken);
            return Results.Created($"/api/v1/administration/branches/{branch.Id}", branch);
        }
        catch (DomainException exception)
        {
            return Results.BadRequest(new { error = "VALIDATION", message = exception.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "CONFLICT" });
        }
    }

    private static async Task<IResult> UpdateBranchAsync(
        Guid branchId,
        UpdateBranchRequest request,
        HttpContext context,
        IAdministrationModule module,
        AuthorizationService authorization,
        ExportAuditWriter auditWriter,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(
                context, authorization, Permissions.AdministrationBranchManage, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var principal = Principal(context);
        var unitOfWork = UnitOfWork(context);
        var before = await module.GetBranchAsync(unitOfWork, branchId, cancellationToken);
        if (before is null)
        {
            await AuditNotFoundAsync(
                auditWriter, unitOfWork, principal, "branch", branchId, context.TraceIdentifier, cancellationToken);
            return Results.NotFound(new { error = "NOT_FOUND" });
        }

        try
        {
            var after = await module.UpdateBranchAsync(
                unitOfWork,
                new UpdateBranchCommand(
                    branchId,
                    request.Code,
                    request.Name,
                    request.Address,
                    request.IsActive,
                    request.Version),
                cancellationToken);
            await new AuditService().RecordAsync(
                unitOfWork,
                principal,
                "administration.branch.updated",
                "branch",
                branchId,
                before,
                after,
                context.TraceIdentifier,
                cancellationToken);
            return Results.Ok(after);
        }
        catch (AdministrationNotFoundException)
        {
            await AuditNotFoundAsync(
                auditWriter, unitOfWork, principal, "branch", branchId, context.TraceIdentifier, cancellationToken);
            return Results.NotFound(new { error = "NOT_FOUND" });
        }
        catch (AdministrationVersionConflictException)
        {
            return Results.Conflict(new { error = "VERSION_CONFLICT" });
        }
        catch (DomainException exception)
        {
            return Results.BadRequest(new { error = "VALIDATION", message = exception.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "CONFLICT" });
        }
    }

    private static async Task<IResult> CreatePartyAsync(
        CreatePartyRequest request,
        HttpContext context,
        IAdministrationModule module,
        AuthorizationService authorization,
        CancellationToken cancellationToken)
    {
        if (!await AuthorizeAsync(
                context, authorization, Permissions.AdministrationPartyManage, cancellationToken))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var principal = Principal(context);
        var unitOfWork = UnitOfWork(context);
        try
        {
            var party = await module.CreatePartyAsync(
                unitOfWork,
                new CreatePartyCommand(request.Name, request.TaxId, request.IsCustomer, request.IsSupplier),
                cancellationToken);
            await new AuditService().RecordAsync(
                unitOfWork,
                principal,
                "administration.party.created",
                "party",
                party.Id,
                before: null,
                after: party,
                context.TraceIdentifier,
                cancellationToken);
            return Results.Created($"/api/v1/administration/parties/{party.Id}", party);
        }
        catch (DomainException exception)
        {
            return Results.BadRequest(new { error = "VALIDATION", message = exception.Message });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = "CONFLICT" });
        }
    }

    private static async Task<bool> AuthorizeAsync(
        HttpContext context,
        AuthorizationService authorization,
        string permission,
        CancellationToken cancellationToken)
    {
        if (context.Items[typeof(SessionPrincipal)] is not SessionPrincipal principal
            || context.Items[typeof(IUnitOfWork)] is not IUnitOfWork unitOfWork)
        {
            return false;
        }

        try
        {
            await authorization.EnsureAllowedAsync(
                unitOfWork,
                principal,
                permission,
                correlationId: context.TraceIdentifier,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (AuthorizationException)
        {
            return false;
        }
    }

    private static async Task AuditNotFoundAsync(
        ExportAuditWriter writer,
        IUnitOfWork unitOfWork,
        SessionPrincipal principal,
        string entityType,
        Guid entityId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        await writer.RecordAsync(
            unitOfWork.TenantId,
            principal.UserId,
            "security.resource.not_found",
            correlationId,
            new { entityType, requestedId = entityId },
            cancellationToken,
            entityType);
    }

    private static SessionPrincipal Principal(HttpContext context)
        => (SessionPrincipal)context.Items[typeof(SessionPrincipal)]!;

    private static IUnitOfWork UnitOfWork(HttpContext context)
        => (IUnitOfWork)context.Items[typeof(IUnitOfWork)]!;
}
