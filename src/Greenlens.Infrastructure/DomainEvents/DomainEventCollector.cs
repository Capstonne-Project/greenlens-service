using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;

namespace Greenlens.Infrastructure.DomainEvents;

internal sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly List<IDomainEvent> _events = [];

    public void Enqueue(IReadOnlyList<IDomainEvent> events) => _events.AddRange(events);

    public IReadOnlyList<IDomainEvent> DrainAll()
    {
        if (_events.Count == 0)
            return [];

        var copy = _events.ToList();
        _events.Clear();
        return copy;
    }

    public void Clear() => _events.Clear();
}
