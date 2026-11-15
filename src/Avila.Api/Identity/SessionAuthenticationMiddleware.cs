using Avila.Platform.Identity;
using Avila.Platform.Tenancy;

namespace Avila.Api.Identity;

public sealed class SessionAuthenticationMiddleware
{
    public const string CookieName = "avila_session";
    private readonly RequestDelegate _next;

    public SessionAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        IdentityService identityService)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : context.Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var principal = await identityService.AuthenticateAsync(
            token,
            context.RequestAborted);

        if (principal is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        tenantContext.SetTenant(principal.TenantId);
        context.Items[typeof(SessionPrincipal)] = principal;
        await _next(context);
    }
}
