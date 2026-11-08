using Avila.Platform.Identity;

namespace Avila.Api.Identity;

public sealed record LoginRequest(string Email, string Password, Guid? TenantId);
public sealed record AcceptInvitationRequest(string Token, string DisplayName, string Password);
public sealed record MfaVerifyRequest(Guid ChallengeId, string Code);
public sealed record SwitchTenantRequest(Guid TenantId);

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/invitations/accept", async (
            AcceptInvitationRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var accepted = await service.AcceptInvitationAsync(
                    request.Token,
                    request.DisplayName,
                    request.Password,
                    cancellationToken);
                return Results.Ok(accepted);
            }
            catch (IdentityException exception)
            {
                return Results.BadRequest(new { error = exception.Code, message = exception.Message });
            }
        });

        group.MapPost("/login", async (
            LoginRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.LoginAsync(
                request.Email,
                request.Password,
                request.TenantId,
                cancellationToken);

            return result.Status is LoginStatus.InvalidCredentials or LoginStatus.LockedOut
                ? Results.Json(result, statusCode: StatusCodes.Status401Unauthorized)
                : Results.Ok(result);
        });

        group.MapPost("/mfa/verify", async (
            MfaVerifyRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.VerifyMfaAsync(request.ChallengeId, request.Code, cancellationToken);
            return result.Status == LoginStatus.Authenticated
                ? Results.Ok(result)
                : Results.Json(result, statusCode: StatusCodes.Status401Unauthorized);
        });

        group.MapPost("/switch-tenant", async (
            HttpContext context,
            SwitchTenantRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            var token = BearerToken(context);
            if (token is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                var newToken = await service.SwitchTenantAsync(token, request.TenantId, cancellationToken);
                return Results.Ok(new { token = newToken });
            }
            catch (IdentityException exception)
            {
                return Results.BadRequest(new { error = exception.Code, message = exception.Message });
            }
        });

        group.MapPost("/logout", async (
            HttpContext context,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            var token = BearerToken(context);
            if (token is null)
            {
                return Results.Unauthorized();
            }

            await service.RevokeSessionAsync(token, cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/mfa/enroll", async (
            HttpContext context,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            var principal = await AuthenticateAsync(context, service, cancellationToken);
            if (principal is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(await service.EnrollMfaAsync(principal.UserId, cancellationToken));
        });

        group.MapPost("/mfa/confirm", async (
            HttpContext context,
            MfaVerifyRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            var principal = await AuthenticateAsync(context, service, cancellationToken);
            if (principal is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                await service.ConfirmMfaAsync(principal.UserId, request.Code, cancellationToken);
                return Results.NoContent();
            }
            catch (IdentityException exception)
            {
                return Results.BadRequest(new { error = exception.Code, message = exception.Message });
            }
        });

        return app;
    }

    private static async Task<SessionPrincipal?> AuthenticateAsync(
        HttpContext context,
        IdentityService service,
        CancellationToken cancellationToken)
    {
        var token = BearerToken(context);
        return token is null ? null : await service.AuthenticateAsync(token, cancellationToken);
    }

    private static string? BearerToken(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : null;
    }
}
