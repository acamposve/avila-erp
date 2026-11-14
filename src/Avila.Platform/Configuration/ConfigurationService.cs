using Avila.Platform.Authorization;
using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Configuration;

/// <summary>
/// Parameter versions are append-only. Selection uses effective_from/effective_to;
/// historical rows are never updated or deleted.
/// </summary>
public sealed class ConfigurationService
{
    private readonly AuthorizationService _authorization;
    private readonly AuditService _audit;

    public ConfigurationService(AuthorizationService authorization, AuditService audit)
    {
        _authorization = authorization;
        _audit = audit;
    }

    public async Task<Guid> PublishAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        string key,
        string value,
        string source,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(source))
        {
            throw new DomainException("Setting key, value and source are required.");
        }

        if (effectiveTo.HasValue && effectiveTo <= effectiveFrom)
        {
            throw new DomainException("Effective-to must be after effective-from.");
        }

        await _authorization.EnsureAllowedAsync(
            unitOfWork,
            actor,
            Permissions.AdministrationSettingsManage,
            cancellationToken: cancellationToken);

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var overlaps = await context.SettingVersions.AnyAsync(
            x => x.TenantId == unitOfWork.TenantId
                 && x.Key == key
                 && x.EffectiveFrom < (effectiveTo ?? DateTimeOffset.MaxValue)
                 && (x.EffectiveTo == null || x.EffectiveTo > effectiveFrom),
            cancellationToken);

        if (overlaps)
        {
            throw new DomainException("A setting version already overlaps this effective range.");
        }

        var version = new SettingVersion
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            Key = key.Trim(),
            Value = value,
            Source = source.Trim(),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CreatedBy = actor.UserId
        };

        context.SettingVersions.Add(version);
        await context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(
            unitOfWork,
            actor,
            "setting.version.published",
            "setting_version",
            version.Id,
            before: null,
            after: new { version.Key, version.Value, version.Source, version.EffectiveFrom, version.EffectiveTo },
            correlationId: version.Id.ToString("N"),
            cancellationToken);

        return version.Id;
    }

    public async Task<SettingVersion?> GetEffectiveAsync(
        IUnitOfWork unitOfWork,
        string key,
        DateTimeOffset? at = null,
        CancellationToken cancellationToken = default)
    {
        var instant = at ?? DateTimeOffset.UtcNow;
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        return await context.SettingVersions
            .Where(x => x.TenantId == unitOfWork.TenantId
                        && x.Key == key
                        && x.EffectiveFrom <= instant
                        && (x.EffectiveTo == null || x.EffectiveTo > instant))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SettingVersion>> GetHistoryAsync(
        IUnitOfWork unitOfWork,
        string key,
        CancellationToken cancellationToken = default)
    {
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        return await context.SettingVersions
            .Where(x => x.TenantId == unitOfWork.TenantId && x.Key == key)
            .OrderBy(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task SetPreferenceAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        await _authorization.EnsureAllowedAsync(
            unitOfWork,
            actor,
            Permissions.AdministrationSettingsManage,
            cancellationToken: cancellationToken);

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var setting = await context.TenantSettings
            .SingleOrDefaultAsync(x => x.TenantId == unitOfWork.TenantId && x.Key == key, cancellationToken);

        var previousValue = setting?.Value;
        if (setting is null)
        {
            setting = new TenantSetting
            {
                Id = Guid.NewGuid(),
                TenantId = unitOfWork.TenantId,
                Key = key,
                Value = value
            };
            context.TenantSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
        }

        await context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(
            unitOfWork,
            actor,
            "tenant.preference.updated",
            "tenant_setting",
            setting.Id,
            before: previousValue is null ? null : new { Value = previousValue },
            after: new { key, value },
            correlationId: Guid.NewGuid().ToString("N"),
            cancellationToken);
    }
}
