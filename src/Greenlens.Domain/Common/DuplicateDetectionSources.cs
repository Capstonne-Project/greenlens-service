namespace Greenlens.Domain.Common;

/// <summary>
/// Values for <see cref="Entities.Report.DuplicateDetectionSource"/> (BR-REP-030/031).
/// </summary>
public static class DuplicateDetectionSources
{
    /// <summary>Tier 1: geo ≤25m + same category (inline on submit).</summary>
    public const string Tier1 = "geo_category";

    /// <summary>Tier 2: Tier 1 + AI image compare confirmed same scene.</summary>
    public const string Tier2Ai = "geo_category_ai";

    /// <summary>Legacy Tier 1 value (pre-rename, may exist in DB).</summary>
    public const string Tier1Legacy = "geo_time";

    /// <summary>Legacy Tier 2 value (pre-rename, may exist in DB).</summary>
    public const string Tier2AiLegacy = "geo_time_ai";

    /// <summary>True when Tier 2 AI job may still run (Tier 1 flag, not yet upgraded).</summary>
    public static bool IsTier1PendingAi(string? source) =>
        source is Tier1 or Tier1Legacy;
}
