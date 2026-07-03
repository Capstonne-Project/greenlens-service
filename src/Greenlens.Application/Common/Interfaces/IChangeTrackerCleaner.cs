namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Clears EF Core change tracker between the main command commit and deferred side effects
/// (notifications, gamification) so re-entrant SaveChanges does not replay stale entities.
/// </summary>
public interface IChangeTrackerCleaner
{
    void ClearTrackedEntities();
}
