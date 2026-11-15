using Avila.Platform.Identity;
using Microsoft.EntityFrameworkCore;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class MfaSessionTests
{
    private readonly ProductionFixture _fixture;

    public MfaSessionTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Owner_requires_mfa_enrollment_and_recovery_codes_are_hashed_and_single_use()
    {
        var service = Service();
        var tenant = Guid.NewGuid();
        var email = Email("owner-mfa");
        var ticket = await service.InviteAsync(tenant, email, "owner");
        var accepted = await service.AcceptInvitationAsync(ticket.Token, "Owner", Password);

        var required = await service.LoginAsync(email, Password, tenant);
        Assert.Equal(LoginStatus.MfaEnrollmentRequired, required.Status);
        Assert.Null(required.Token);
        Assert.NotNull(required.MfaChallengeId);

        var enrollment = await service.BeginMfaEnrollmentAsync(required.MfaChallengeId!.Value);
        var completed = await service.CompleteMfaEnrollmentAsync(
            required.MfaChallengeId.Value,
            Totp.Code(enrollment.Secret));
        Assert.Equal(LoginStatus.Authenticated, completed.Status);
        Assert.NotNull(completed.Token);
        Assert.NotNull(completed.RecoveryCodes);
        Assert.Equal(10, completed.RecoveryCodes!.Count);

        await using (var context = new IdentityDbContext(Options()))
        {
            var stored = await context.MfaRecoveryCodes
                .Where(x => x.UserId == accepted.UserId)
                .ToListAsync();
            Assert.Equal(10, stored.Count);
            Assert.DoesNotContain(stored, x => x.CodeHash == completed.RecoveryCodes[0]);
        }

        var secondLogin = await service.LoginAsync(email, Password, tenant);
        Assert.Equal(LoginStatus.MfaRequired, secondLogin.Status);
        var recovery = await service.VerifyMfaAsync(
            secondLogin.MfaChallengeId!.Value,
            completed.RecoveryCodes[0]);
        Assert.Equal(LoginStatus.Authenticated, recovery.Status);
        Assert.NotNull(recovery.Token);

        var replay = await service.LoginAsync(email, Password, tenant);
        var replayAttempt = await service.VerifyMfaAsync(
            replay.MfaChallengeId!.Value,
            completed.RecoveryCodes[0]);
        Assert.Equal(LoginStatus.InvalidCredentials, replayAttempt.Status);
    }

    [Fact]
    public async Task Idle_absolute_expiry_role_change_and_replay_invalidate_sessions()
    {
        var service = Service();
        var tenant = Guid.NewGuid();
        var email = Email("sessions");
        var ticket = await service.InviteAsync(tenant, email, "owner");
        var accepted = await service.AcceptInvitationAsync(ticket.Token, "Sessions", Password);
        var secret = await EnrollAsync(service, email, tenant);

        var login = await LoginMfaAsync(service, email, tenant, secret);
        Assert.NotNull(await service.AuthenticateAsync(login.Token!));

        await using (var context = new IdentityDbContext(Options()))
        {
            var session = await context.Sessions.SingleAsync(
                x => x.TokenHash == SessionTokens.Hash(login.Token!));
            session.LastSeenAt = DateTimeOffset.UtcNow.AddMinutes(-31);
            await context.SaveChangesAsync();
        }

        Assert.Null(await service.AuthenticateAsync(login.Token!));

        var second = await LoginMfaAsync(service, email, tenant, secret);
        await using (var context = new IdentityDbContext(Options()))
        {
            var session = await context.Sessions.SingleAsync(
                x => x.TokenHash == SessionTokens.Hash(second.Token!));
            session.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await context.SaveChangesAsync();
        }

        Assert.Null(await service.AuthenticateAsync(second.Token!));

        var third = await LoginMfaAsync(service, email, tenant, secret);
        await service.ChangeRoleAsync(accepted.UserId, tenant, "member");
        Assert.Null(await service.AuthenticateAsync(third.Token!));
    }

    [Fact]
    public async Task Step_up_marks_session_and_expires_after_five_minutes()
    {
        var service = Service();
        var tenant = Guid.NewGuid();
        var email = Email("stepup");
        var ticket = await service.InviteAsync(tenant, email, "owner");
        await service.AcceptInvitationAsync(ticket.Token, "Step Up", Password);
        var secret = await EnrollAsync(service, email, tenant);
        var login = await LoginMfaAsync(service, email, tenant, secret);

        await using (var context = new IdentityDbContext(Options()))
        {
            var session = await context.Sessions.SingleAsync(
                x => x.TokenHash == SessionTokens.Hash(login.Token!));
            session.SteppedUpAt = null;
            await context.SaveChangesAsync();
        }

        Assert.True(await service.StepUpAsync(login.Token!, Totp.Code(secret)));
        var principal = await service.AuthenticateAsync(login.Token!);
        Assert.NotNull(principal);
        Assert.NotNull(principal.SteppedUpAt);
    }

    private IdentityService Service() => new(_fixture.IdentityConnectionString, _fixture.IdentityMfaKey);

    private async Task<string> EnrollAsync(IdentityService service, string email, Guid tenant)
    {
        var required = await service.LoginAsync(email, Password, tenant);
        Assert.Equal(LoginStatus.MfaEnrollmentRequired, required.Status);
        var enrollment = await service.BeginMfaEnrollmentAsync(required.MfaChallengeId!.Value);
        var completed = await service.CompleteMfaEnrollmentAsync(
            required.MfaChallengeId.Value,
            Totp.Code(enrollment.Secret));
        Assert.Equal(LoginStatus.Authenticated, completed.Status);
        return enrollment.Secret;
    }

    private static async Task<LoginResult> LoginMfaAsync(
        IdentityService service,
        string email,
        Guid tenant,
        string secret)
    {
        var login = await service.LoginAsync(email, Password, tenant);
        Assert.Equal(LoginStatus.MfaRequired, login.Status);
        return await service.VerifyMfaAsync(
            login.MfaChallengeId!.Value,
            Totp.Code(secret));
    }

    private DbContextOptions<IdentityDbContext> Options()
        => new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_fixture.IdentityConnectionString)
            .Options;

    private static string Email(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.test";
    private const string Password = "CorrectHorseBattery1!";
}
