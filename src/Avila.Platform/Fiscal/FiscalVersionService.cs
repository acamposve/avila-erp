using System.Text.Json;
using Avila.Platform.Authorization;
using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;
using Avila.SharedKernel;

namespace Avila.Platform.Fiscal;

public sealed record FiscalRuleVersionDto(
    Guid Id,
    string RuleKey,
    int VersionNumber,
    string ValueJson,
    string Source,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo);

public sealed record FiscalDocumentSnapshotDto(
    Guid Id,
    string DocumentType,
    Guid DocumentId,
    string RuleKey,
    Guid FiscalRuleVersionId,
    int RuleVersion,
    DateTimeOffset CapturedAt);

/// <summary>
/// Immutable fiscal rule versions and document snapshots. Rule values are
/// opaque, data-driven JSON; this service never supplies tax rates or legal
/// values of its own.
/// </summary>
public sealed class FiscalVersionService
{
    private readonly AuthorizationService _authorization;
    private readonly AuditService _audit;

    public FiscalVersionService(
        AuthorizationService? authorization = null,
        AuditService? audit = null)
    {
        _authorization = authorization ?? new AuthorizationService();
        _audit = audit ?? new AuditService();
    }

    public async Task<FiscalRuleVersionDto> PublishRuleAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        string ruleKey,
        string valueJson,
        string source,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ruleKey)
            || string.IsNullOrWhiteSpace(valueJson)
            || string.IsNullOrWhiteSpace(source))
        {
            throw new DomainException("Rule key, value JSON and source are required.");
        }

        if (effectiveTo.HasValue && effectiveTo <= effectiveFrom)
        {
            throw new DomainException("Effective-to must be after effective-from.");
        }

        try
        {
            using var _ = JsonDocument.Parse(valueJson);
        }
        catch (JsonException)
        {
            throw new DomainException("Rule value must be valid JSON.");
        }

        await _authorization.EnsureAllowedAsync(
            unitOfWork,
            actor,
            Permissions.AdministrationSettingsManage,
            correlationId: Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var overlaps = await context.FiscalRuleVersions.AnyAsync(
            x => x.TenantId == unitOfWork.TenantId
                 && x.RuleKey == ruleKey
                 && x.EffectiveFrom < (effectiveTo ?? DateTimeOffset.MaxValue)
                 && (x.EffectiveTo == null || x.EffectiveTo > effectiveFrom),
            cancellationToken);
        if (overlaps)
        {
            throw new DomainException("A fiscal rule version already overlaps this effective range.");
        }

        var versionNumber = (await context.FiscalRuleVersions
            .Where(x => x.TenantId == unitOfWork.TenantId && x.RuleKey == ruleKey)
            .MaxAsync(x => (int?)x.VersionNumber, cancellationToken) ?? 0) + 1;

        var version = new FiscalRuleVersion
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            RuleKey = ruleKey.Trim(),
            VersionNumber = versionNumber,
            ValueJson = valueJson,
            Source = source.Trim(),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            CreatedBy = actor.UserId
        };
        context.FiscalRuleVersions.Add(version);
        await context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(
            unitOfWork,
            actor,
            "fiscal.rule.published",
            "fiscal_rule_version",
            version.Id,
            before: null,
            after: new
            {
                version.RuleKey,
                version.VersionNumber,
                version.ValueJson,
                version.Source,
                version.EffectiveFrom,
                version.EffectiveTo
            },
            correlationId: version.Id.ToString("N"),
            cancellationToken);

        return ToDto(version);
    }

    public async Task<FiscalRuleVersionDto?> GetEffectiveAsync(
        IUnitOfWork unitOfWork,
        string ruleKey,
        DateTimeOffset? at = null,
        CancellationToken cancellationToken = default)
    {
        var instant = at ?? DateTimeOffset.UtcNow;
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var version = await context.FiscalRuleVersions
            .AsNoTracking()
            .Where(x => x.TenantId == unitOfWork.TenantId
                        && x.RuleKey == ruleKey
                        && x.EffectiveFrom <= instant
                        && (x.EffectiveTo == null || x.EffectiveTo > instant))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        return version is null ? null : ToDto(version);
    }

    public async Task<FiscalDocumentSnapshotDto> CaptureSnapshotAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        string documentType,
        Guid documentId,
        string ruleKey,
        DateTimeOffset? at = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(ruleKey))
        {
            throw new DomainException("Document type and rule key are required.");
        }

        await _authorization.EnsureAllowedAsync(
            unitOfWork,
            actor,
            Permissions.AdministrationSettingsManage,
            correlationId: Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);

        var effective = await GetEffectiveAsync(unitOfWork, ruleKey, at, cancellationToken);
        if (effective is null)
        {
            throw new DomainException($"No effective fiscal rule version for {ruleKey}.");
        }

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var existing = await context.FiscalDocumentSnapshots.AnyAsync(
            x => x.TenantId == unitOfWork.TenantId
                 && x.DocumentType == documentType
                 && x.DocumentId == documentId
                 && x.RuleKey == ruleKey,
            cancellationToken);
        if (existing)
        {
            throw new DomainException("A fiscal snapshot already exists for this document and rule.");
        }

        var snapshot = new FiscalDocumentSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            DocumentType = documentType.Trim(),
            DocumentId = documentId,
            RuleKey = ruleKey.Trim(),
            FiscalRuleVersionId = effective.Id,
            RuleVersion = effective.VersionNumber,
            CapturedBy = actor.UserId,
            CapturedAt = at ?? DateTimeOffset.UtcNow
        };
        context.FiscalDocumentSnapshots.Add(snapshot);
        await context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(
            unitOfWork,
            actor,
            "fiscal.snapshot.captured",
            "fiscal_document_snapshot",
            snapshot.Id,
            before: null,
            after: new
            {
                snapshot.DocumentType,
                snapshot.DocumentId,
                snapshot.RuleKey,
                snapshot.FiscalRuleVersionId,
                snapshot.RuleVersion
            },
            correlationId: snapshot.Id.ToString("N"),
            cancellationToken);

        return new FiscalDocumentSnapshotDto(
            snapshot.Id,
            snapshot.DocumentType,
            snapshot.DocumentId,
            snapshot.RuleKey,
            snapshot.FiscalRuleVersionId,
            snapshot.RuleVersion,
            snapshot.CapturedAt);
    }

    public async Task<FiscalDocumentSnapshotDto?> GetSnapshotAsync(
        IUnitOfWork unitOfWork,
        string documentType,
        Guid documentId,
        string ruleKey,
        CancellationToken cancellationToken = default)
    {
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);
        var snapshot = await context.FiscalDocumentSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TenantId == unitOfWork.TenantId
                     && x.DocumentType == documentType
                     && x.DocumentId == documentId
                     && x.RuleKey == ruleKey,
                cancellationToken);
        return snapshot is null
            ? null
            : new FiscalDocumentSnapshotDto(
                snapshot.Id,
                snapshot.DocumentType,
                snapshot.DocumentId,
                snapshot.RuleKey,
                snapshot.FiscalRuleVersionId,
                snapshot.RuleVersion,
                snapshot.CapturedAt);
    }

    private static FiscalRuleVersionDto ToDto(FiscalRuleVersion version)
        => new(
            version.Id,
            version.RuleKey,
            version.VersionNumber,
            version.ValueJson,
            version.Source,
            version.EffectiveFrom,
            version.EffectiveTo);
}
