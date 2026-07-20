using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Infrastructure.Persistence;

/// <summary>
/// UnitOfWork that dispatches domain events via MediatR after persisting changes.
/// Events are collected from tracked entities BEFORE SaveChanges, then published AFTER
/// so that handlers see the committed state.
/// </summary>
internal sealed class UnitOfWork(
    ApplicationDbContext context,
    IPublisher publisher,
    ITransactionManager transactionManager,
    IDomainEventCollector eventCollector) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Collect domain events from all tracked entities before saving
        var domainEvents = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .SelectMany(e =>
            {
                var events = e.Entity.DomainEvents.ToList();
                e.Entity.ClearDomainEvents();
                return events;
            })
            .ToList();

        var result = await context.SaveChangesAsync(ct).ConfigureAwait(false);

        if (domainEvents.Count == 0)
            return result;

        // Inside an open transaction: defer handlers until commit to avoid
        // re-entrant SaveChanges (e.g. VerifyReport → AwardPoints concurrency).
        if (transactionManager.HasActiveTransaction)
        {
            eventCollector.Enqueue(domainEvents);
            return result;
        }

        await PublishEventsAsync(domainEvents, ct).ConfigureAwait(false);

        return result;
    }

    internal async Task PublishEventsAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken ct)
    {
        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, ct).ConfigureAwait(false);
        }
    }
}
