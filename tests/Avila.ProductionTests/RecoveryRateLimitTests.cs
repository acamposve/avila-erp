using Avila.Api.Identity;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Avila.ProductionTests;

[Collection("production-postgres")]
public sealed class RecoveryRateLimitTests
{
    private const string Password = "CorrectHorseBattery1!";
    private readonly ProductionFixture _fixture;

    public RecoveryRateLimitTests(ProductionFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Password_reset_is_single_use_enumeration_safe_and_revokes_sessions()
    {
        var sink = new SinkEmailSender();
        var service = Service(sink);
        var tenant = Guid.NewGuid();
        var email = Email("reset");
        var invitation = await service.InviteAsync(tenant, email, "member");
        await service.AcceptInvitationAsync(invitation.Token, "Reset User", Password);
        var login = await service.LoginAsync(email, Password, tenant);
        Assert.NotNull(login.Token);

        await service.RequestPasswordResetAsync(email);
        Assert.Single(sink.Messages);
        var token = TokenFrom(sink.Messages[0].Text);

        await service.RequestPasswordResetAsync("missing@example.test");
        Assert.Single(sink.Messages);

        await service.ResetPasswordAsync(token, "NewCorrectHorse1!");
        Assert.Null(await service.AuthenticateAsync(login.Token!));

        var newLogin = await service.LoginAsync(email, "NewCorrectHorse1!", tenant);
        Assert.Equal(LoginStatus.Authenticated, newLogin.Status);

        await Assert.ThrowsAsync<IdentityException>(
            () => service.ResetPasswordAsync(token, "AnotherCorrectHorse1!"));
    }

    [Fact]
    public async Task Email_verification_token_is_single_use_and_hashed()
    {
        var sink = new SinkEmailSender();
        var service = Service(sink);
        var tenant = Guid.NewGuid();
        var email = Email("verify");
        var invitation = await service.InviteAsync(tenant, email, "member");
        var accepted = await service.AcceptInvitationAsync(invitation.Token, "Verify User", Password);

        await using (var context = new IdentityDbContext(Options()))
        {
            var user = await context.Users.SingleAsync(x => x.Id == accepted.UserId);
            user.EmailVerified = false;
            await context.SaveChangesAsync();
        }

        await service.RequestEmailVerificationAsync(email);
        Assert.Single(sink.Messages);
        var token = TokenFrom(sink.Messages[0].Text);

        await using (var context = new IdentityDbContext(Options()))
        {
            var stored = await context.EmailVerificationTokens
                .SingleAsync(x => x.UserId == accepted.UserId);
            Assert.NotEqual(token, stored.TokenHash);
        }

        await service.VerifyEmailAsync(token);
        await using (var context = new IdentityDbContext(Options()))
        {
            Assert.True((await context.Users.SingleAsync(x => x.Id == accepted.UserId)).EmailVerified);
        }

        await Assert.ThrowsAsync<IdentityException>(() => service.VerifyEmailAsync(token));
    }

    [Fact]
    public async Task Rate_limiter_returns_429_and_retry_after()
    {
        var limiter = new AuthRateLimiter();
        var middleware = new AuthRateLimitingMiddleware(
            _ => Task.CompletedTask,
            limiter,
            NullLogger<AuthRateLimitingMiddleware>.Instance);

        DefaultHttpContext? blocked = null;
        for (var index = 0; index < 11; index++)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/v1/auth/login";
            context.Response.Body = new MemoryStream();
            await middleware.InvokeAsync(context);
            if (index < 10)
            {
                Assert.NotEqual(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
            }
            else
            {
                blocked = context;
            }
        }

        Assert.NotNull(blocked);
        Assert.Equal(StatusCodes.Status429TooManyRequests, blocked!.Response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(blocked.Response.Headers.RetryAfter.ToString()));
    }

    [Fact]
    public async Task Failed_login_is_security_audited()
    {
        var tenant = Guid.NewGuid();
        var email = Email("audit");
        var baseService = Service(new SinkEmailSender());
        var invitation = await baseService.InviteAsync(tenant, email, "member");
        var accepted = await baseService.AcceptInvitationAsync(invitation.Token, "Audit User", Password);

        var audited = new IdentityService(
            _fixture.IdentityConnectionString,
            _fixture.IdentityMfaKey,
            new SinkEmailSender(),
            new IdentityAuditWriter(new TenantUnitOfWorkFactory(), _fixture.AppConnectionString));
        var failed = await audited.LoginAsync(email, "WrongPassword1!", tenant);
        Assert.Equal(LoginStatus.InvalidCredentials, failed.Status);

        await using var connection = new NpgsqlConnection(_fixture.AdminConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT COUNT(*) FROM platform.audit_events
            WHERE tenant_id = @tenant
              AND actor_user_id = @user
              AND action = 'auth.login.failed'
            """,
            connection);
        command.Parameters.AddWithValue("tenant", tenant);
        command.Parameters.AddWithValue("user", accepted.UserId);
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync() ?? 0L));
    }

    private IdentityService Service(SinkEmailSender sink)
        => new(_fixture.IdentityConnectionString, _fixture.IdentityMfaKey, sink);

    private DbContextOptions<IdentityDbContext> Options()
        => new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_fixture.IdentityConnectionString)
            .Options;

    private static string TokenFrom(string text)
        => text[(text.LastIndexOf(' ') + 1)..].Trim();

    private static string Email(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.test";
}
