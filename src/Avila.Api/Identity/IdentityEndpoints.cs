using Avila.Platform.Identity;

namespace Avila.Api.Identity;

public sealed record LoginRequest(string Email, string Password, Guid? TenantId);
public sealed record AcceptInvitationRequest(string Token, string DisplayName, string Password);
public sealed record MfaVerifyRequest(Guid ChallengeId, string Code);
public sealed record SwitchTenantRequest(Guid TenantId);
public sealed record StepUpRequest(string Code);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string Password);
public sealed record EmailVerificationRequest(string Token);

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
            HttpContext context,
            LoginRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.LoginAsync(
                request.Email,
                request.Password,
                request.TenantId,
                cancellationToken);

            if (result.Status == LoginStatus.Authenticated && result.Token is not null)
            {
                SetSessionCookie(context, result.Token, result.ExpiresAt);
            }

            return result.Status is LoginStatus.InvalidCredentials or LoginStatus.LockedOut
                ? Results.Json(result, statusCode: StatusCodes.Status401Unauthorized)
                : Results.Ok(result);
        });

        group.MapPost("/mfa/enroll/begin", async (
            MfaVerifyRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.BeginMfaEnrollmentAsync(
                    request.ChallengeId,
                    cancellationToken));
            }
            catch (IdentityException exception)
            {
                return Results.BadRequest(new { error = exception.Code, message = exception.Message });
            }
        });

        group.MapPost("/mfa/enroll/complete", async (
            HttpContext context,
            MfaVerifyRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CompleteMfaEnrollmentAsync(
                request.ChallengeId,
                request.Code,
                cancellationToken);

            if (result.Status == LoginStatus.Authenticated && result.Token is not null)
            {
                SetSessionCookie(context, result.Token, result.ExpiresAt);
            }

            return result.Status == LoginStatus.Authenticated
                ? Results.Ok(result)
                : Results.Json(result, statusCode: StatusCodes.Status401Unauthorized);
        });

        group.MapPost("/password/forgot", async (
            ForgotPasswordRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            await service.RequestPasswordResetAsync(request.Email, cancellationToken);
            return Results.Accepted();
        });

        group.MapPost("/password/reset", async (
            ResetPasswordRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await service.ResetPasswordAsync(request.Token, request.Password, cancellationToken);
                return Results.NoContent();
            }
            catch (IdentityException exception)
            {
                return Results.BadRequest(new { error = exception.Code, message = exception.Message });
            }
        });

        group.MapPost("/email/verification/request", async (
            ForgotPasswordRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            await service.RequestEmailVerificationAsync(request.Email, cancellationToken);
            return Results.Accepted();
        });

        group.MapPost("/email/verification/confirm", async (
            EmailVerificationRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await service.VerifyEmailAsync(request.Token, cancellationToken);
                return Results.NoContent();
            }
            catch (IdentityException exception)
            {
                return Results.BadRequest(new { error = exception.Code, message = exception.Message });
            }
        });

        group.MapPost("/step-up", async (
            HttpContext context,
            StepUpRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            var token = BearerToken(context);
            if (token is null)
            {
                return Results.Unauthorized();
            }

            return await service.StepUpAsync(token, request.Code, cancellationToken)
                ? Results.NoContent()
                : Results.Unauthorized();
        });

        group.MapPost("/mfa/verify", async (
            HttpContext context,
            MfaVerifyRequest request,
            IdentityService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.VerifyMfaAsync(request.ChallengeId, request.Code, cancellationToken);
            if (result.Status == LoginStatus.Authenticated && result.Token is not null)
            {
                SetSessionCookie(context, result.Token, result.ExpiresAt);
            }

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
            : context.Request.Cookies[SessionAuthenticationMiddleware.CookieName];
    }

    private static void SetSessionCookie(
        HttpContext context,
        string token,
        DateTimeOffset? expiresAt)
    {
        context.Response.Cookies.Append(
            SessionAuthenticationMiddleware.CookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
                Path = "/api"
            });
    }
}
