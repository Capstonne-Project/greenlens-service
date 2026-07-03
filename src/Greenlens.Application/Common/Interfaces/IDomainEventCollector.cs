using Greenlens.Domain.Common;

namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Buffers domain events raised during an open DB transaction so handlers
/// (gamification, notifications) run after commit — avoids re-entrant SaveChanges.
/// </summary>
public interface IDomainEventCollector
{
    void Enqueue(IReadOnlyList<IDomainEvent> events);

    IReadOnlyList<IDomainEvent> DrainAll();

    void Clear();
}
