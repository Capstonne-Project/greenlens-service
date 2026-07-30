using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common;

/// <summary>Closed report snapshot for BR-REP-034 recurrence detection.</summary>
public readonly record struct ViolationRecurrenceNearbyReport(
    Guid Id,
    decimal Latitude,
    decimal Longitude,
    DateTime ClosedAt);

/// <summary>
/// Picks the most recently Closed report within 50m and same category (BR-REP-034).
/// </summary>
public static class ViolationRecurrencePrimarySelector
{
    public const double DefaultRadiusMeters = 50.0;
    public static readonly TimeSpan LookbackWindow = TimeSpan.FromDays(30);

    public static Guid? SelectPrimary(
        decimal reportLatitude,
        decimal reportLongitude,
        IEnumerable<ViolationRecurrenceNearbyReport> nearby,
        double radiusMeters = DefaultRadiusMeters)
    {
        var match = nearby
            .Where(c => GeoMath.HaversineMeters(reportLatitude, reportLongitude, c.Latitude, c.Longitude) <= radiusMeters)
            .OrderByDescending(c => c.ClosedAt)
            .FirstOrDefault();

        return match.Id == Guid.Empty ? null : match.Id;
    }
}
