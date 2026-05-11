namespace Greenlens.Application.Common.Map;

/// <summary>Validation caps for public map viewport queries (BR-REP-003 bbox, BR-SYS performance).</summary>
public static class PublicMapQueryLimits
{
    public const decimal MinLatitudeVn = 8m;
    public const decimal MaxLatitudeVn = 24m;
    public const decimal MinLongitudeVn = 102m;
    public const decimal MaxLongitudeVn = 110m;

    /// <summary>Reject bbox wider than this latitude span (degrees) — forces zoom-in for detail queries.</summary>
    public const decimal MaxBoundingLatSpan = 6m;

    /// <summary>Reject bbox wider than this longitude span (degrees).</summary>
    public const decimal MaxBoundingLngSpan = 8m;

    public const int DefaultDetailLimit = 200;
    public const int MaxDetailLimit = 500;

    public const int DefaultGridLevel = 3;
    public const int MinGridLevel = 1;
    public const int MaxGridLevel = 5;

    /// <summary>Max raw points loaded into memory for aggregate grouping per request.</summary>
    public const int MaxRowsForAggregateGrouping = 50_000;

    /// <summary>Grid fineness: higher level = smaller cells (degrees).</summary>
    public static decimal CellSizeDegrees(int gridLevel) =>
        gridLevel switch
        {
            1 => 0.5m,
            2 => 0.25m,
            3 => 0.1m,
            4 => 0.05m,
            5 => 0.02m,
            _ => 0.1m
        };
}
