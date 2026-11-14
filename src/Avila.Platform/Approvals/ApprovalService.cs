using Avila.Platform.Authorization;
using Avila.Platform.Identity;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Avila.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Approvals;

public sealed class ApprovalRequiredException : Exception
{
    public ApprovalRequiredException(string type, decimal amount)
        : base($"Approval is required for {type} at {amount}.")
    {
        Type = type;
        Amount = amount;
    }

    public string Type { get; }
    public decimal Amount { get; }
}

/// <summary>
/// Type/amount approval rules. A requester cannot approve their own request;
/// missing or pending approval blocks the operation.
/// </summary>
public sealed class ApprovalService
{
    private readonly AuthorizationService _authorization;
    private readonly AuditService _audit;

    public ApprovalService(AuthorizationService authorization, AuditService audit)
    {
        _authorization = authorization;
        _audit = audit;
    }

    public async Task<Guid> ConfigureRuleAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        string type,
        decimal thresholdAmount,
        string requiredRole = "owner",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(type) || thresholdAmount < 0)
        {
            throw new DomainException("Approval type and non-negative threshold are required.");
        }

        await _authorization.EnsureAllowedAsync(
            unitOfWork,
            actor,
            Permissions.AdministrationApprovalManage,
            cancellationToken: cancellationToken);

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var rule = await context.ApprovalRules
            .SingleOrDefaultAsync(x => x.TenantId == unitOfWork.TenantId && x.Type == type, cancellationToken);

        if (rule is null)
        {
            rule = new ApprovalRule
            {
                Id = Guid.NewGuid(),
                TenantId = unitOfWork.TenantId,
                Type = type,
                ThresholdAmount = thresholdAmount,
                RequiredRole = requiredRole
            };
            context.ApprovalRules.Add(rule);
        }
        else
        {
            rule.ThresholdAmount = thresholdAmount;
            rule.RequiredRole = requiredRole;
            rule.IsActive = true;
        }

        await context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(
            unitOfWork,
            actor,
            "approval.rule.configured",
            "approval_rule",
            rule.Id,
            before: null,
            after: new { rule.Type, rule.ThresholdAmount, rule.RequiredRole },
            correlationId: rule.Id.ToString("N"),
            cancellationToken);

        return rule.Id;
    }

    public async Task<Guid> RequestAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        string type,
        Guid entityId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(reason) || amount < 0)
        {
            throw new DomainException("Approval type, reason and non-negative amount are required.");
        }

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var request = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            Type = type,
            EntityId = entityId,
            Amount = amount,
            Status = "pending",
            RequestedBy = actor.UserId,
            Reason = reason.Trim()
        };

        context.ApprovalRequests.Add(request);
        await context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(
            unitOfWork,
            actor,
            "approval.requested",
            "approval_request",
            request.Id,
            before: null,
            after: new { request.Type, request.EntityId, request.Amount, request.Reason },
            correlationId: request.Id.ToString("N"),
            cancellationToken);

        return request.Id;
    }

    public async Task DecideAsync(
        IUnitOfWork unitOfWork,
        SessionPrincipal actor,
        Guid requestId,
        bool approve,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("An approval decision requires a reason.");
        }

        await _authorization.EnsureAllowedAsync(
            unitOfWork,
            actor,
            Permissions.AdministrationApprovalManage,
            cancellationToken: cancellationToken);

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var request = await context.ApprovalRequests
            .SingleOrDefaultAsync(x => x.Id == requestId, cancellationToken);

        if (request is null || request.Status != "pending")
        {
            throw new DomainException("Approval request is not pending.");
        }

        if (request.RequestedBy == actor.UserId)
        {
            throw new DomainException("A requester cannot approve their own request.");
        }

        request.Status = approve ? "approved" : "rejected";
        request.DecidedBy = actor.UserId;
        request.DecidedAt = DateTimeOffset.UtcNow;
        request.Reason = reason.Trim();
        await context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(
            unitOfWork,
            actor,
            approve ? "approval.approved" : "approval.rejected",
            "approval_request",
            request.Id,
            before: new { Status = "pending" },
            after: new { request.Status, request.DecidedBy, request.Reason },
            correlationId: request.Id.ToString("N"),
            cancellationToken);
    }

    public async Task EnsureApprovedAsync(
        IUnitOfWork unitOfWork,
        string type,
        Guid entityId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var rule = await context.ApprovalRules
            .SingleOrDefaultAsync(
                x => x.TenantId == unitOfWork.TenantId && x.Type == type && x.IsActive,
                cancellationToken);

        if (rule is null || amount < rule.ThresholdAmount)
        {
            return;
        }

        var approved = await context.ApprovalRequests.AnyAsync(
            x => x.TenantId == unitOfWork.TenantId
                 && x.Type == type
                 && x.EntityId == entityId
                 && x.Amount == amount
                 && x.Status == "approved",
            cancellationToken);

        if (!approved)
        {
            throw new ApprovalRequiredException(type, amount);
        }
    }
}
