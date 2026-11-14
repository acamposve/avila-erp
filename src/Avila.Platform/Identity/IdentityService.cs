using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Identity;

public enum LoginStatus
{
    Authenticated,
    MfaRequired,
    TenantSelectionRequired,
    InvalidCredentials,
    LockedOut,
    NoMembership,
    TenantAccessDenied
}

public sealed record MembershipInfo(Guid TenantId, string Role);

public sealed record LoginResult(
    LoginStatus Status,
    string? Token = null,
    DateTimeOffset? ExpiresAt = null,
    Guid? UserId = null,
    Guid? TenantId = null,
    Guid? MfaChallengeId = null,
    IReadOnlyList<MembershipInfo>? Memberships = null);

public sealed record InvitationTicket(Guid InvitationId, string Token, DateTimeOffset ExpiresAt);

public sealed record AcceptedMembership(Guid UserId, Guid TenantId, string Role);

public sealed record SessionPrincipal(Guid UserId, Guid TenantId, string Email, string Role);

public sealed record MfaEnrollment(string Secret, string OtpAuthUri);

/// <summary>
/// Identity/session service. It uses the dedicated identity role and connection;
/// it never runs inside a tenant RLS transaction.
/// </summary>
public sealed class IdentityService
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    private static readonly TimeSpan MfaLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockoutLifetime = TimeSpan.FromMinutes(15);
    private const int MaxFailedAttempts = 5;
    private static readonly HashSet<string> Roles = new(StringComparer.Ordinal)
    {
        "owner", "admin", "member"
    };

    private readonly string _connectionString;
    private readonly byte[] _mfaKey;

    public IdentityService(string connectionString, string mfaSecretKey)
    {
        _connectionString = connectionString;
        _mfaKey = SecretProtector.ParseKey(mfaSecretKey);
    }

    public async Task<InvitationTicket> InviteAsync(
        Guid tenantId,
        string email,
        string role,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        ValidateRole(role);

        var token = SessionTokens.New();
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.Trim(),
            NormalizedEmail = normalized,
            Role = role,
            TokenHash = SessionTokens.Hash(token),
            ExpiresAt = DateTimeOffset.UtcNow.Add(InvitationLifetime)
        };

        await using var context = CreateContext();
        context.Invitations.Add(invitation);
        await context.SaveChangesAsync(cancellationToken);

        return new InvitationTicket(invitation.Id, token, invitation.ExpiresAt);
    }

    public async Task<AcceptedMembership> AcceptInvitationAsync(
        string token,
        string displayName,
        string password,
        CancellationToken cancellationToken = default)
    {
        PasswordHasher.ValidatePolicy(password);
        var hash = SessionTokens.Hash(token);
        var now = DateTimeOffset.UtcNow;

        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var invitation = await context.Invitations
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (invitation is null || invitation.AcceptedAt.HasValue || invitation.ExpiresAt <= now)
        {
            throw new IdentityException("INVITATION_INVALID", "Invitation is invalid or expired.");
        }

        var existing = await context.Users
            .SingleOrDefaultAsync(x => x.NormalizedEmail == invitation.NormalizedEmail, cancellationToken);

        if (existing is not null)
        {
            throw new IdentityException(
                "USER_ALREADY_EXISTS",
                "An account with this email already exists; sign in and request access instead.");
        }

        var user = new IdentityUser
        {
            Id = Guid.NewGuid(),
            Email = invitation.Email,
            NormalizedEmail = invitation.NormalizedEmail,
            DisplayName = displayName.Trim(),
            PasswordHash = PasswordHasher.Hash(password)
        };

        context.Users.Add(user);
        context.Memberships.Add(new Membership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = invitation.TenantId,
            Role = invitation.Role
        });

        invitation.AcceptedAt = now;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AcceptedMembership(user.Id, invitation.TenantId, invitation.Role);
    }

    public async Task<LoginResult> LoginAsync(
        string email,
        string password,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var normalized = NormalizeEmail(email);
        var now = DateTimeOffset.UtcNow;

        var user = await context.Users
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalized, cancellationToken);

        if (user is null)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        if (user.LockedUntil.HasValue && user.LockedUntil > now)
        {
            return new LoginResult(LoginStatus.LockedOut, UserId: user.Id);
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            user.FailedAttempts++;
            if (user.FailedAttempts >= MaxFailedAttempts)
            {
                user.LockedUntil = now.Add(LockoutLifetime);
            }

            await context.SaveChangesAsync(cancellationToken);
            return new LoginResult(
                user.LockedUntil.HasValue && user.LockedUntil > now
                    ? LoginStatus.LockedOut
                    : LoginStatus.InvalidCredentials,
                UserId: user.Id);
        }

        user.FailedAttempts = 0;
        user.LockedUntil = null;
        await context.SaveChangesAsync(cancellationToken);

        var memberships = await context.Memberships
            .Where(x => x.UserId == user.Id && x.Status == "active")
            .OrderBy(x => x.TenantId)
            .Select(x => new MembershipInfo(x.TenantId, x.Role))
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return new LoginResult(LoginStatus.NoMembership, UserId: user.Id);
        }

        Guid selectedTenant;
        MembershipInfo selectedMembership;

        if (tenantId.HasValue)
        {
            var membership = memberships.SingleOrDefault(x => x.TenantId == tenantId.Value);
            if (membership is null)
            {
                return new LoginResult(
                    LoginStatus.TenantAccessDenied,
                    UserId: user.Id,
                    Memberships: memberships);
            }

            selectedTenant = membership.TenantId;
            selectedMembership = membership;
        }
        else if (memberships.Count == 1)
        {
            selectedTenant = memberships[0].TenantId;
            selectedMembership = memberships[0];
        }
        else
        {
            return new LoginResult(
                LoginStatus.TenantSelectionRequired,
                UserId: user.Id,
                Memberships: memberships);
        }

        if (user.MfaEnabled)
        {
            var challenge = new MfaChallenge
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TenantId = selectedTenant,
                ExpiresAt = now.Add(MfaLifetime)
            };
            context.MfaChallenges.Add(challenge);
            await context.SaveChangesAsync(cancellationToken);

            return new LoginResult(
                LoginStatus.MfaRequired,
                UserId: user.Id,
                TenantId: selectedTenant,
                MfaChallengeId: challenge.Id);
        }

        var token = await CreateSessionAsync(context, user, selectedTenant, cancellationToken);
        return new LoginResult(
            LoginStatus.Authenticated,
            Token: token.Token,
            ExpiresAt: token.ExpiresAt,
            UserId: user.Id,
            TenantId: selectedTenant,
            Memberships: memberships);
    }

    public async Task GrantMembershipAsync(
        Guid userId,
        Guid tenantId,
        string role,
        CancellationToken cancellationToken = default)
    {
        ValidateRole(role);
        await using var context = CreateContext();

        var userExists = await context.Users.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            throw new IdentityException("USER_NOT_FOUND", "User does not exist.");
        }

        var exists = await context.Memberships
            .AnyAsync(x => x.UserId == userId && x.TenantId == tenantId, cancellationToken);
        if (exists)
        {
            throw new IdentityException("MEMBERSHIP_EXISTS", "User already belongs to this tenant.");
        }

        context.Memberships.Add(new Membership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Role = role
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<LoginResult> VerifyMfaAsync(
        Guid challengeId,
        string code,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;

        var challenge = await context.MfaChallenges
            .SingleOrDefaultAsync(x => x.Id == challengeId, cancellationToken);

        if (challenge is null || challenge.ConsumedAt.HasValue || challenge.ExpiresAt <= now)
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        var user = await context.Users
            .SingleOrDefaultAsync(x => x.Id == challenge.UserId, cancellationToken);

        if (user?.MfaSecret is null || !Totp.Verify(UnprotectMfa(user.MfaSecret), code, now))
        {
            return new LoginResult(LoginStatus.InvalidCredentials);
        }

        challenge.ConsumedAt = now;
        var token = await CreateSessionAsync(context, user, challenge.TenantId, cancellationToken);

        return new LoginResult(
            LoginStatus.Authenticated,
            Token: token.Token,
            ExpiresAt: token.ExpiresAt,
            UserId: user.Id,
            TenantId: challenge.TenantId);
    }

    public async Task<SessionPrincipal?> AuthenticateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var hash = SessionTokens.Hash(token);
        var now = DateTimeOffset.UtcNow;

        await using var context = CreateContext();
        var session = await context.Sessions
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (session is null || session.RevokedAt.HasValue || session.ExpiresAt <= now)
        {
            return null;
        }

        var membership = await context.Memberships
            .SingleOrDefaultAsync(
                x => x.UserId == session.UserId
                     && x.TenantId == session.ActiveTenantId
                     && x.Status == "active",
                cancellationToken);

        if (membership is null)
        {
            return null;
        }

        var user = await context.Users
            .SingleAsync(x => x.Id == session.UserId, cancellationToken);

        session.LastSeenAt = now;
        await context.SaveChangesAsync(cancellationToken);

        return new SessionPrincipal(user.Id, membership.TenantId, user.Email, membership.Role);
    }

    public async Task<string> SwitchTenantAsync(
        string token,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var hash = SessionTokens.Hash(token);
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;

        var session = await context.Sessions
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (session is null || session.RevokedAt.HasValue || session.ExpiresAt <= now)
        {
            throw new IdentityException("SESSION_INVALID", "Session is invalid or expired.");
        }

        var membership = await context.Memberships
            .SingleOrDefaultAsync(
                x => x.UserId == session.UserId
                     && x.TenantId == tenantId
                     && x.Status == "active",
                cancellationToken);

        if (membership is null)
        {
            throw new IdentityException("TENANT_ACCESS_DENIED", "The user is not a member of this tenant.");
        }

        var user = await context.Users.SingleAsync(x => x.Id == session.UserId, cancellationToken);
        session.RevokedAt = now;

        var newToken = await CreateSessionAsync(context, user, tenantId, cancellationToken);
        return newToken.Token;
    }

    public async Task RevokeSessionAsync(string token, CancellationToken cancellationToken = default)
    {
        var hash = SessionTokens.Hash(token);
        await using var context = CreateContext();
        var session = await context.Sessions
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (session is null)
        {
            return;
        }

        session.RevokedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MfaEnrollment> EnrollMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var user = await context.Users.SingleAsync(x => x.Id == userId, cancellationToken);
        var secret = Totp.NewSecret();
        user.MfaSecret = SecretProtector.Protect(secret, _mfaKey);
        user.MfaEnabled = false;
        await context.SaveChangesAsync(cancellationToken);

        return new MfaEnrollment(secret, Totp.OtpAuthUri("ÁVILA ERP", user.Email, secret));
    }

    public async Task ConfirmMfaAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        var user = await context.Users.SingleAsync(x => x.Id == userId, cancellationToken);

        if (user.MfaSecret is null || !Totp.Verify(UnprotectMfa(user.MfaSecret), code))
        {
            throw new IdentityException("MFA_INVALID", "MFA code is invalid.");
        }

        user.MfaEnabled = true;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MembershipInfo>> GetMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var context = CreateContext();
        return await context.Memberships
            .Where(x => x.UserId == userId && x.Status == "active")
            .OrderBy(x => x.TenantId)
            .Select(x => new MembershipInfo(x.TenantId, x.Role))
            .ToListAsync(cancellationToken);
    }

    private IdentityDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new IdentityDbContext(options);
    }

    private static async Task<(string Token, DateTimeOffset ExpiresAt)> CreateSessionAsync(
        IdentityDbContext context,
        IdentityUser user,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var token = SessionTokens.New();
        var expiresAt = DateTimeOffset.UtcNow.Add(SessionLifetime);
        context.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ActiveTenantId = tenantId,
            TokenHash = SessionTokens.Hash(token),
            ExpiresAt = expiresAt
        });
        await context.SaveChangesAsync(cancellationToken);
        return (token, expiresAt);
    }

    private string UnprotectMfa(string protectedSecret)
    {
        try
        {
            return SecretProtector.Unprotect(protectedSecret, _mfaKey);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            throw new IdentityException("MFA_UNAVAILABLE", "MFA secret cannot be read.");
        }
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new IdentityException("EMAIL_INVALID", "A valid email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static void ValidateRole(string role)
    {
        if (!Roles.Contains(role))
        {
            throw new IdentityException("ROLE_INVALID", "Role must be owner, admin or member.");
        }
    }
}
