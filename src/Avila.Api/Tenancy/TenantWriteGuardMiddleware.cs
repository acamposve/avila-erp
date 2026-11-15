using Avila.Platform.Exporting;
using Avila.Platform.Identity;
using Avila.Platform.Lifecycle;
using Avila.Platform.Tenancy;

namespace Avila.Api.Tenancy;

/// <summary>
/// Blocks all mutating API calls for suspended tenants before handlers run.
/// Safe reads, auth and export remain available.
/// </summary>
public sealed class TenantWriteGuardMiddleware
{
    private readonly RequestDelegate _next;

    public TenantWriteGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantAccessGuard accessGuard,
        ExportAuditWriter auditWriter)
    {
        var method = context.Request.Method;
        var isMutation = HttpMethods.IsPost(method)
                         || HttpMethods.IsPut(method)
                         || HttpMethods.IsPatch(method)
                         || HttpMethods.IsDelete(method);

        if (!isMutation
            || context.Items[typeof(IUnitOfWork)] is not IUnitOfWork unitOfWork
            || context.Items[typeof(SessionPrincipal)] is not SessionPrincipal principal)
        {
            await _next(context);
            return;
        }

        try
        {
            await accessGuard.EnsureWritesAllowedAsync(unitOfWork, context.RequestAborted);
        }
        catch (TenantSuspendedException)
        {
            await auditWriter.RecordAsync(
                unitOfWork.TenantId,
                principal.UserId,
                "tenant.write.blocked",
                context.TraceIdentifier,
                new { method, path = context.Request.Path.Value },
                context.RequestAborted,
                entityType: "tenant");
            context.Response.StatusCode = StatusCodes.Status423Locked;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "TENANT_SUSPENDED",
                message = "Tenant is suspended; writes are blocked."
            });
            return;
        }

        await _next(context);
    }
}

public static class TenantWriteGuardExtensions
{
    public static IApplicationBuilder UseTenantWriteGuard(this IApplicationBuilder app)
        => app.UseMiddleware<TenantWriteGuardMiddleware>();
}
