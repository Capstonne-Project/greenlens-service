using Greenlens.Domain.Entities;

namespace Greenlens.Application.Features.Gamification;

internal static class GamificationNotificationPlaceholders
{
    internal static Dictionary<string, string> ForBadgeEarned(Badge badge) =>
        new()
        {
            ["badge_name"] = badge.NameVi
        };

    internal static Dictionary<string, string> ForLevelUp(int level) =>
        new()
        {
            ["level"] = level.ToString()
        };

    internal static Dictionary<string, string> Empty { get; } = new();
}
