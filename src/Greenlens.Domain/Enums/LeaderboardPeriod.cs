namespace Greenlens.Domain.Enums;

/// <summary>
/// Leaderboard time period filter (BR-GAM-005).
/// <see cref="AllTime"/> ranks by lifetime total points; others filter by transaction window.
/// </summary>
public enum LeaderboardPeriod
{
    AllTime,
    Weekly,
    Monthly,
    Yearly
}
