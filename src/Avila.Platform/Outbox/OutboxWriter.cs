using System.Text.Json;
using Avila.Platform.Persistence;
using Avila.Platform.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Avila.Platform.Outbox;

public sealed class OutboxWriter
{
    public async Task<Guid> EnqueueAsync(
        IUnitOfWork unitOfWork,
        string eventType,
        object payload,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        await using var context = new PlatformDbContext(
            unitOfWork.OptionsFor<PlatformDbContext>("platform"));
        unitOfWork.Enlist(context);

        var outbox = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            TenantId = unitOfWork.TenantId,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload),
            CorrelationId = correlationId,
            OccurredAt = DateTimeOffset.UtcNow
        };
        context.OutboxEvents.Add(outbox);
        await context.SaveChangesAsync(cancellationToken);
        return outbox.Id;
    }
}
