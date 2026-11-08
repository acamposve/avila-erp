using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Identity;

public sealed class IdentityUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? MfaSecret { get; set; }
    public bool MfaEnabled { get; set; }
    public int FailedAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Membership> Memberships { get; set; } = [];
}

public sealed class Membership
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Role { get; set; } = "member";
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Invitation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string Role { get; set; } = "member";
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Session
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ActiveTenantId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class MfaChallenge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Identity persistence. Identity tables are intentionally not tenant-RLS:
/// authentication must resolve a user and their memberships before a tenant is
/// selected. Only the dedicated avila_identity role is granted access.
/// </summary>
public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    public DbSet<IdentityUser> Users => Set<IdentityUser>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<MfaChallenge> MfaChallenges => Set<MfaChallenge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityUser>(entity =>
        {
            entity.ToTable("users", "identity");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(320);
            entity.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320);
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(200);
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500);
            entity.Property(x => x.MfaSecret).HasColumnName("mfa_secret").HasMaxLength(256);
            entity.Property(x => x.MfaEnabled).HasColumnName("mfa_enabled");
            entity.Property(x => x.FailedAttempts).HasColumnName("failed_attempts");
            entity.Property(x => x.LockedUntil).HasColumnName("locked_until");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
            entity.HasMany(x => x.Memberships)
                .WithOne()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Membership>(entity =>
        {
            entity.ToTable("memberships", "identity");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(32);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(24);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.UserId, x.TenantId }).IsUnique();
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.ToTable("invitations", "identity");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(320);
            entity.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320);
            entity.Property(x => x.Role).HasColumnName("role").HasMaxLength(32);
            entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(128);
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.Property(x => x.AcceptedAt).HasColumnName("accepted_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.TokenHash).IsUnique();
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("sessions", "identity");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.ActiveTenantId).HasColumnName("active_tenant_id");
            entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(128);
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.LastSeenAt).HasColumnName("last_seen_at");
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ActiveTenantId });
        });

        modelBuilder.Entity<MfaChallenge>(entity =>
        {
            entity.ToTable("mfa_challenges", "identity");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.Property(x => x.ConsumedAt).HasColumnName("consumed_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.UserId, x.ExpiresAt });
        });
    }
}
