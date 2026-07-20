namespace Greenlens.Application.Features.Map.GetMapViewportSummary;

public sealed record MapViewportSummaryResponse(
    int ReportCount,
    int Days,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<MapViewportDailyCountDto> DailyCounts);

/// <summary>One bar in the home chart (reports created on that UTC calendar date).</summary>
public sealed record MapViewportDailyCountDto(
    DateOnly Date,
    int Count);
