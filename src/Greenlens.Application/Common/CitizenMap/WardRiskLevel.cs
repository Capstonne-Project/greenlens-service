namespace Greenlens.Application.Common.CitizenMap;

/// <summary>
/// 5-tier ward risk level for the citizen map ward drill-down (Bước 2 — province → ward view).
/// Computed by BE from the count of currently-active (unresolved) reports in the ward; FE only
/// renders the <c>Level</c>/<c>ColorHex</c> the API returns, it never recomputes thresholds.
/// </summary>
public enum WardRiskLevel
{
    /// <summary>No active reports.</summary>
    None = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Critical = 5
}

/// <summary>
/// Maps an active-report count to a <see cref="WardRiskLevel"/> and a display color. Thresholds
/// are intentionally simple counts (not percentages) — wards are small administrative units so
/// raw active-report count is a reasonable proxy for "how urgent is this area right now".
/// </summary>
public static class WardRiskLevelCalculator
{
    public static WardRiskLevel FromActiveReportCount(int activeReportCount) => activeReportCount switch
    {
        <= 0 => WardRiskLevel.None,
        <= 2 => WardRiskLevel.Low,
        <= 5 => WardRiskLevel.Medium,
        <= 10 => WardRiskLevel.High,
        _ => WardRiskLevel.Critical
    };

    /// <summary>Hex color for map fill/legend — green (calm) to dark red (critical).</summary>
    public static string ColorHexFor(WardRiskLevel level) => level switch
    {
        WardRiskLevel.None => "#94A3B8",
        WardRiskLevel.Low => "#22C55E",
        WardRiskLevel.Medium => "#EAB308",
        WardRiskLevel.High => "#F97316",
        WardRiskLevel.Critical => "#DC2626",
        _ => "#94A3B8"
    };
}
