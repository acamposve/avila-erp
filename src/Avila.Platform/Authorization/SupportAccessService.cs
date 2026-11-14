using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Authorization;

/// <summary>
/// Time-boxed, read-only-by-default platform support access. Every grant and
/// revocation is audited in the same transaction.
/// </summary>
public sealed class SupportAccessService
{
    private readonly AuditService _auditService;

    public SupportAccessService(AuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task<Guid> GrantAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        Guid supportUserId,
        string ticket,
        string justification,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticket) || string.IsNullOrWhiteSpace(justification))
        {
            throw new DomainException("Support access requires a ticket and justification.");
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new DomainException("Support access must expire in the future.");
        }

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var grant = new SupportAccessGrant
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            SupportUserId = supportUserId,
            Ticket = ticket.Trim(),
            Justification = justification.Trim(),
            IsReadOnly = true,
            ExpiresAt = expiresAt
        };

        context.SupportAccessGrants.Add(grant);
        await context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            unitOfWork,
            actor,
            "support.access.granted",
            "support_access_grant",
            grant.Id,
            before: null,
            after: new { grant.Ticket, grant.Justification, grant.ExpiresAt, grant.IsReadOnly },
            correlationId: grant.Id.ToString("N"),
            cancellationToken);

        return grant.Id;
    }

    public async Task<bool> HasAccessAsync(
        IUnitOfWork unitOfWork,
        Guid supportUserId,
        CancellationToken cancellationToken = default)
    {
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var now = DateTimeOffset.UtcNow;
        return await context.SupportAccessGrants.AnyAsync(
            x => x.TenantId == unitOfWork.TenantId
                 && x.SupportUserId == supportUserId
                 && x.RevokedAt == null
                 && x.ExpiresAt > now,
            cancellationToken);
    }

    public async Task RevokeAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        Guid grantId,
        CancellationToken cancellationToken = default)
    {
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var grant = await context.SupportAccessGrants
            .SingleOrDefaultAsync(x => x.Id == grantId, cancellationToken);

        if (grant is null)
        {
            throw new DomainException("Support access grant was not found.");
        }

        grant.RevokedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            unitOfWork,
            actor,
            "support.access.revoked",
            "support_access_grant",
            grant.Id,
            before: new { grant.ExpiresAt },
            after: new { grant.RevokedAt },
            correlationId: grant.Id.ToString("N"),
            cancellationToken);
    }
}
