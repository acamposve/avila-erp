using Avila.Platform.Exporting;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;

namespace Avila.Api.Exporting;

public static class TenantExportEndpoint
{
    public static async Task HandleAsync(
        HttpContext context,
        TenantExportRegistry registry,
        ExportAuditWriter auditWriter,
        CancellationToken cancellationToken = default)
    {
        if (context.Items[typeof(SessionPrincipal)] is not SessionPrincipal principal)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (principal.Role is not ("owner" or "admin"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (context.Request.Query.ContainsKey("tenantId")
            || context.Request.RouteValues.ContainsKey("tenantId"))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "TENANT_OVERRIDE_NOT_ALLOWED",
                message = "Tenant is derived from the authenticated context."
            }, cancellationToken);
            return;
        }

        if (context.Items[typeof(IUnitOfWork)] is not IUnitOfWork unitOfWork
            || unitOfWork.TenantId != principal.TenantId)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var correlationId = context.TraceIdentifier;
        await auditWriter.RecordAsync(
            unitOfWork.TenantId,
            principal.UserId,
            "tenant.export.requested",
            correlationId,
            new { format = TenantExportFormat.Version },
            cancellationToken);

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/x-ndjson; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";

        try
        {
            var manifest = await registry.StreamNdjsonAsync(
                unitOfWork,
                context.Response.Body,
                cancellationToken);

            await auditWriter.RecordAsync(
                unitOfWork.TenantId,
                principal.UserId,
                "tenant.export.completed",
                correlationId,
                new
                {
                    format = manifest.FormatVersion,
                    sha256 = manifest.Sha256,
                    sections = manifest.Sections.Select(section => new
                    {
                        module = section.ModuleKey,
                        records = section.RecordCount,
                        sha256 = section.Sha256
                    })
                },
                cancellationToken);
        }
        catch
        {
            try
            {
                await auditWriter.RecordAsync(
                    unitOfWork.TenantId,
                    principal.UserId,
                    "tenant.export.failed",
                    correlationId,
                    new { format = TenantExportFormat.Version },
                    CancellationToken.None);
            }
            catch
            {
                // Preserve the original failure; the failed audit is best-effort.
            }

            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            throw;
        }
    }
}
