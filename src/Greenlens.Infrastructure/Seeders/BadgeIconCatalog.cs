namespace Greenlens.Infrastructure.Seeders;

/// <summary>Canonical badge icon paths on R2 (BR-GAM-004). Source PNGs: docs/UserBadge/icons/.</summary>
internal static class BadgeIconCatalog
{
    internal const string DefaultPublicBase = "https://pub-d1de759d41364ae7890b5d1273065f8c.r2.dev";
    internal const string IconObjectPrefix = "badges/icons";

    /// <summary>Active badge codes — one PNG per code in docs/UserBadge/icons/.</summary>
    internal static readonly string[] ActiveCodes =
    [
        "first_report",
        "eco_warrior",
        "green_champion",
        "earth_guardian",
        "streak_7d",
        "streak_30d",
        "duplicate_finder",
        "community_voice",
        "cleanup_hero",
        "rising_star",
        "eco_expert",
        "green_legend"
    ];

    /// <summary>Retired badges removed from catalog (no hotspot feature).</summary>
    internal static readonly string[] RetiredCodes = ["hotspot_hunter"];

    internal static string BuildIconUrl(string code, string? publicBase = null) =>
        $"{(publicBase ?? DefaultPublicBase).TrimEnd('/')}/{IconObjectPrefix}/{code}.png";

    internal static string BuildObjectKey(string code) =>
        $"{IconObjectPrefix}/{code}.png";
}
