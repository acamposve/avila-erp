using Avila.Platform.Authorization;
using Avila.Platform.Exporting;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;

namespace Avila.Api.Audit;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/admin/audit", QueryAsync);
        app.MapGet("/api/v1/admin/audit/export", ExportAsync);
        app.MapGet("/api/v1/admin/audit/verify", VerifyAsync);
        return app;
    }

    public static async Task<IResult> QueryAsync(
        HttpContext context,
        AuthorizationService authorization,
        AuditQueryService queryService,
        ExportAuditWriter auditWriter,
        CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(context, authorization, cancellationToken))
        {
            return Results.Forbid();
        }

        var principal = (SessionPrincipal)context.Items[typeof(SessionPrincipal)]!;
        var unitOfWork = (IUnitOfWork)context.Items[typeof(IUnitOfWork)]!;
        var query = ParseQuery(context);
        await auditWriter.RecordAsync(
            unitOfWork.TenantId,
            principal.UserId,
            "audit.queried",
            context.TraceIdentifier,
            new { query.From, query.To, query.Action, query.ActorUserId, query.EntityType },
            cancellationToken,
            entityType: "audit");

        var page = await queryService.QueryAsync(
            unitOfWork,
            query,
            context.Request.Query["cursor"],
            ParseLimit(context),
            cancellationToken);
        return Results.Ok(page);
    }

    public static async Task<IResult> ExportAsync(
        HttpContext context,
        AuthorizationService authorization,
        AuditQueryService queryService,
        ExportAuditWriter auditWriter,
        CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(context, authorization, cancellationToken))
        {
            return Results.Forbid();
        }

        var principal = (SessionPrincipal)context.Items[typeof(SessionPrincipal)]!;
        var unitOfWork = (IUnitOfWork)context.Items[typeof(IUnitOfWork)]!;
        var query = ParseQuery(context);
        await auditWriter.RecordAsync(
            unitOfWork.TenantId,
            principal.UserId,
            "audit.exported",
            context.TraceIdentifier,
            new { query.From, query.To, query.Action, query.ActorUserId, query.EntityType },
            cancellationToken,
            entityType: "audit");

        context.Response.ContentType = "application/x-ndjson; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        var count = await queryService.ExportNdjsonAsync(
            unitOfWork,
            query,
            context.Response.Body,
            cancellationToken);
        return Results.Empty;
    }

    public static async Task<IResult> VerifyAsync(
        HttpContext context,
        AuthorizationService authorization,
        AuditIntegrityService integrity,
        ExportAuditWriter auditWriter,
        CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(context, authorization, cancellationToken))
        {
            return Results.Forbid();
        }

        var principal = (SessionPrincipal)context.Items[typeof(SessionPrincipal)]!;
        var unitOfWork = (IUnitOfWork)context.Items[typeof(IUnitOfWork)]!;
        var result = await integrity.VerifyAsync(unitOfWork, cancellationToken);
        await auditWriter.RecordAsync(
            unitOfWork.TenantId,
            principal.UserId,
            "audit.verified",
            context.TraceIdentifier,
            new { result.IsValid, result.CheckedEvents },
            cancellationToken,
            entityType: "audit");
        return Results.Ok(result);
    }

    private static async Task<bool> IsAuthorizedAsync(
        HttpContext context,
        AuthorizationService authorization,
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
                Permissions.AdministrationAuditRead,
                correlationId: context.TraceIdentifier,
                cancellationToken: cancellationToken);
            return true;
        }
        catch (AuthorizationException)
        {
            return false;
        }
    }

    private static AuditQuery ParseQuery(HttpContext context)
    {
        var q = context.Request.Query;
        return new AuditQuery(
            ParseDate(q["from"]),
            ParseDate(q["to"]),
            q["action"],
            Guid.TryParse(q["actorUserId"], out var actor) ? actor : null,
            q["entityType"]);
    }

    private static DateTimeOffset? ParseDate(string? value)
        => DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

    private static int ParseLimit(HttpContext context)
        => int.TryParse(context.Request.Query["limit"], out var limit) ? limit : 100;
}
