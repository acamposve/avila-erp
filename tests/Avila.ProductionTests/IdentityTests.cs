using Avila.Api.Identity;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class IdentityTests
{
    private const string Password = "CorrectHorseBattery1!";
    private readonly ProductionFixture _fixture;

    public IdentityTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Invitation_acceptance_creates_user_and_membership_and_is_single_use()
    {
        var service = Service();
        var tenant = Guid.NewGuid();
        var email = Email("invite");

        var ticket = await service.InviteAsync(tenant, email, "owner");
        var accepted = await service.AcceptInvitationAsync(ticket.Token, "Invited User", Password);

        Assert.Equal(tenant, accepted.TenantId);
        Assert.Equal("owner", accepted.Role);

        var login = await service.LoginAsync(email, Password, tenant);
        Assert.Equal(LoginStatus.Authenticated, login.Status);
        Assert.NotNull(login.Token);

        var principal = await service.AuthenticateAsync(login.Token!);
        Assert.NotNull(principal);
        Assert.Equal(tenant, principal.TenantId);
        Assert.Equal(email, principal.Email);

        await Assert.ThrowsAsync<IdentityException>(
            () => service.AcceptInvitationAsync(ticket.Token, "Again", Password));
    }

    [Fact]
    public async Task Multi_org_login_requires_tenant_selection_and_switch_revokes_old_token()
    {
        var service = Service();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var email = Email("multi");

        var ticket = await service.InviteAsync(tenantA, email, "admin");
        var accepted = await service.AcceptInvitationAsync(ticket.Token, "Multi Org", Password);
        await service.GrantMembershipAsync(accepted.UserId, tenantB, "member");

        var selection = await service.LoginAsync(email, Password);
        Assert.Equal(LoginStatus.TenantSelectionRequired, selection.Status);
        Assert.NotNull(selection.Memberships);
        Assert.Equal(2, selection.Memberships!.Count);

        var loginA = await service.LoginAsync(email, Password, tenantA);
        Assert.Equal(LoginStatus.Authenticated, loginA.Status);
        Assert.NotNull(loginA.Token);

        var tokenB = await service.SwitchTenantAsync(loginA.Token!, tenantB);
        var principalB = await service.AuthenticateAsync(tokenB);
        Assert.NotNull(principalB);
        Assert.Equal(tenantB, principalB.TenantId);

        Assert.Null(await service.AuthenticateAsync(loginA.Token!));

        await Assert.ThrowsAsync<IdentityException>(
            () => service.SwitchTenantAsync(tokenB, Guid.NewGuid()));
    }

    [Fact]
    public async Task Manipulated_token_and_revoked_session_are_rejected()
    {
        var service = Service();
        var tenant = Guid.NewGuid();
        var email = Email("token");
        var ticket = await service.InviteAsync(tenant, email, "member");
        await service.AcceptInvitationAsync(ticket.Token, "Token User", Password);
        var login = await service.LoginAsync(email, Password, tenant);

        Assert.NotNull(login.Token);
        Assert.Null(await service.AuthenticateAsync("not-a-real-token"));

        await service.RevokeSessionAsync(login.Token!);
        Assert.Null(await service.AuthenticateAsync(login.Token!));
    }

    [Fact]
    public async Task Mfa_enrollment_and_login_require_a_valid_code()
    {
        var service = Service();
        var tenant = Guid.NewGuid();
        var email = Email("mfa");
        var ticket = await service.InviteAsync(tenant, email, "owner");
        var accepted = await service.AcceptInvitationAsync(ticket.Token, "MFA User", Password);

        var enrollment = await service.EnrollMfaAsync(accepted.UserId);
        Assert.False(string.IsNullOrWhiteSpace(enrollment.Secret));
        Assert.Contains("otpauth://", enrollment.OtpAuthUri);

        await service.ConfirmMfaAsync(accepted.UserId, Totp.Code(enrollment.Secret));

        var login = await service.LoginAsync(email, Password, tenant);
        Assert.Equal(LoginStatus.MfaRequired, login.Status);
        Assert.NotNull(login.MfaChallengeId);

        var wrong = await service.VerifyMfaAsync(login.MfaChallengeId!.Value, "000000");
        Assert.Equal(LoginStatus.InvalidCredentials, wrong.Status);

        var verified = await service.VerifyMfaAsync(
            login.MfaChallengeId!.Value,
            Totp.Code(enrollment.Secret));
        Assert.Equal(LoginStatus.Authenticated, verified.Status);
        Assert.NotNull(verified.Token);
        Assert.NotNull(await service.AuthenticateAsync(verified.Token!));
    }

    [Fact]
    public async Task Password_policy_rejects_short_password()
    {
        var service = Service();
        var ticket = await service.InviteAsync(Guid.NewGuid(), Email("policy"), "member");

        var exception = await Assert.ThrowsAsync<IdentityException>(
            () => service.AcceptInvitationAsync(ticket.Token, "Policy User", "short"));

        Assert.Equal("PASSWORD_POLICY", exception.Code);
    }

    [Fact]
    public async Task Account_is_locked_after_five_failed_attempts()
    {
        var service = Service();
        var tenant = Guid.NewGuid();
        var email = Email("lock");
        var ticket = await service.InviteAsync(tenant, email, "member");
        await service.AcceptInvitationAsync(ticket.Token, "Lock User", Password);

        LoginResult? last = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            last = await service.LoginAsync(email, "WrongPassword1!", tenant);
        }

        Assert.Equal(LoginStatus.LockedOut, last!.Status);
        var correctWhileLocked = await service.LoginAsync(email, Password, tenant);
        Assert.Equal(LoginStatus.LockedOut, correctWhileLocked.Status);
    }

    [Fact]
    public async Task Expired_session_is_rejected()
    {
        var service = Service();
        var tenant = Guid.NewGuid();
        var email = Email("expiry");
        var ticket = await service.InviteAsync(tenant, email, "member");
        await service.AcceptInvitationAsync(ticket.Token, "Expiry User", Password);
        var login = await service.LoginAsync(email, Password, tenant);
        Assert.NotNull(login.Token);

        await using var context = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>()
                .UseNpgsql(_fixture.IdentityConnectionString)
                .Options);

        var hash = SessionTokens.Hash(login.Token!);
        var session = await context.Sessions.SingleAsync(x => x.TokenHash == hash);
        session.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        Assert.Null(await service.AuthenticateAsync(login.Token!));
    }

    [Fact]
    public async Task Session_middleware_sets_tenant_and_rejects_invalid_token()
    {
        var service = Service();
        var tenant = Guid.NewGuid();
        var email = Email("middleware");
        var ticket = await service.InviteAsync(tenant, email, "member");
        await service.AcceptInvitationAsync(ticket.Token, "Middleware User", Password);
        var login = await service.LoginAsync(email, Password, tenant);
        Assert.NotNull(login.Token);

        var tenantContext = new TenantContext();
        var called = false;
        var middleware = new SessionAuthenticationMiddleware(context =>
        {
            called = true;
            Assert.Equal(tenant, tenantContext.TenantId);
            return Task.CompletedTask;
        });

        var valid = new DefaultHttpContext();
        valid.Request.Headers.Authorization = $"Bearer {login.Token}";
        await middleware.InvokeAsync(valid, tenantContext, service);
        Assert.True(called);

        var invalidContext = new TenantContext();
        var invalid = new DefaultHttpContext();
        invalid.Request.Headers.Authorization = "Bearer manipulated-token";
        await middleware.InvokeAsync(invalid, invalidContext, service);
        Assert.Equal(StatusCodes.Status401Unauthorized, invalid.Response.StatusCode);
        Assert.False(invalidContext.IsResolved);
    }

    private IdentityService Service() => new(_fixture.IdentityConnectionString, _fixture.IdentityMfaKey);

    private static string Email(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.test";
}
