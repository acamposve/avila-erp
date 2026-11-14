using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Persistence;

public sealed class TenantRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TenantSetting
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AccessScope
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public Guid ScopeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class SupportAccessGrant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SupportUserId { get; set; }
    public string Ticket { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; } = true;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Platform persistence. Tenant settings are tenant-scoped and forced RLS;
/// the tenants table is global and must not be granted to the application role.
/// </summary>
public sealed class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    public DbSet<TenantRecord> Tenants => Set<TenantRecord>();
    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();
    public DbSet<AccessScope> AccessScopes => Set<AccessScope>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<SupportAccessGrant> SupportAccessGrants => Set<SupportAccessGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantRecord>(entity =>
        {
            entity.ToTable("tenants", "platform");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(24);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TenantSetting>(entity =>
        {
            entity.ToTable("tenant_settings", "platform");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.Key).HasColumnName("key").HasMaxLength(120);
            entity.Property(x => x.Value).HasColumnName("value").HasMaxLength(2000);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.Key }).IsUnique();
        });

        modelBuilder.Entity<AccessScope>(entity =>
        {
            entity.ToTable("access_scopes", "platform");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.ScopeType).HasColumnName("scope_type").HasMaxLength(24);
            entity.Property(x => x.ScopeId).HasColumnName("scope_id");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.UserId, x.ScopeType, x.ScopeId }).IsUnique();
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events", "platform");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(120);
            entity.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(120);
            entity.Property(x => x.EntityId).HasColumnName("entity_id");
            entity.Property(x => x.BeforeJson).HasColumnName("before_json").HasColumnType("jsonb");
            entity.Property(x => x.AfterJson).HasColumnName("after_json").HasColumnType("jsonb");
            entity.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(120);
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            entity.HasIndex(x => new { x.TenantId, x.OccurredAt });
        });

        modelBuilder.Entity<SupportAccessGrant>(entity =>
        {
            entity.ToTable("support_access_grants", "platform");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.TenantId).HasColumnName("tenant_id");
            entity.Property(x => x.SupportUserId).HasColumnName("support_user_id");
            entity.Property(x => x.Ticket).HasColumnName("ticket").HasMaxLength(120);
            entity.Property(x => x.Justification).HasColumnName("justification").HasMaxLength(1000);
            entity.Property(x => x.IsReadOnly).HasColumnName("is_read_only");
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.SupportUserId, x.ExpiresAt });
        });
    }
}
