using System.Text.Json;
using Avila.Platform.Identity;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Avila.Platform.Exporting;

/// <summary>
/// Safe identity export. It must run on the dedicated avila_identity connection;
/// it filters by tenant membership and never projects credentials or tokens.
/// </summary>
public sealed class IdentityExportContributor : ITenantExportContributor
{
    private readonly string _identityConnectionString;

    public IdentityExportContributor(string identityConnectionString)
    {
        _identityConnectionString = identityConnectionString;
    }

    public string ModuleKey => "identity";
    public int Order => 20;
    public int SchemaVersion => 1;

    public async Task<TenantExportSection> ExportAsync(
        IUnitOfWork unitOfWork,
        TenantExportContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_identityConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Identity is required for identity export.");
        }

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_identityConnectionString)
            .Options;
        await using var db = new IdentityDbContext(options);

        await EnsureIdentityRoleAsync(db, cancellationToken);

        var memberships = await db.Memberships
            .AsNoTracking()
            .Where(x => x.TenantId == context.TenantId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var userIds = memberships.Select(x => x.UserId).Distinct().ToList();

        var users = userIds.Count == 0
            ? new Dictionary<Guid, IdentityUser>()
            : await db.Users
                .AsNoTracking()
                .Where(x => userIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        var records = new List<TenantExportRecord>();
        foreach (var membership in memberships)
        {
            users.TryGetValue(membership.UserId, out var user);
            records.Add(new TenantExportRecord(
                "membership",
                membership.Id.ToString("D"),
                JsonSerializer.SerializeToElement(new
                {
                    user_id = membership.UserId,
                    role = membership.Role,
                    status = membership.Status,
                    created_at = membership.CreatedAt,
                    user = user is null
                        ? null
                        : new
                        {
                            id = user.Id,
                            email = user.Email,
                            display_name = user.DisplayName,
                            mfa_enabled = user.MfaEnabled,
                            created_at = user.CreatedAt
                        }
                })));
        }

        var invitations = await db.Invitations
            .AsNoTracking()
            .Where(x => x.TenantId == context.TenantId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var invitation in invitations)
        {
            records.Add(new TenantExportRecord(
                "invitation",
                invitation.Id.ToString("D"),
                JsonSerializer.SerializeToElement(new
                {
                    email = invitation.Email,
                    role = invitation.Role,
                    expires_at = invitation.ExpiresAt,
                    accepted_at = invitation.AcceptedAt,
                    created_at = invitation.CreatedAt
                })));
        }

        return new TenantExportSection(ModuleKey, SchemaVersion, records);
    }

    private static async Task EnsureIdentityRoleAsync(
        IdentityDbContext db,
        CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT current_user", connection);
        var role = (string?)await command.ExecuteScalarAsync(cancellationToken);
        if (!string.Equals(role, "avila_identity", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Identity export must use avila_identity; current role is {role ?? "(null)"}.");
        }
    }
}
