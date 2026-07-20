using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Infrastructure.Persistence;

internal sealed class ChangeTrackerCleaner(ApplicationDbContext context) : IChangeTrackerCleaner
{
    public void ClearTrackedEntities() => context.ChangeTracker.Clear();
}
