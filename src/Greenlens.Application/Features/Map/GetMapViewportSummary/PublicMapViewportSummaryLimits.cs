namespace Greenlens.Application.Features.Map.GetMapViewportSummary;

/// <summary>Caps for GET /v1/map/summary.</summary>
public static class PublicMapViewportSummaryLimits
{
    public const int DefaultDays = 30;
    public const int MinDays = 7;
    public const int MaxDays = 90;
}
