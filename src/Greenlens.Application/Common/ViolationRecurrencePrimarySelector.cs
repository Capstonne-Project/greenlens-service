using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common;

/// <summary>Closed report snapshot for BR-REP-034 recurrence detection.</summary>
public readonly record struct ViolationRecurrenceNearbyReport(
    Guid Id,
    decimal Latitude,
    decimal Longitude,
    string? WardCode,
    string? ProvinceCode,
    DateTime ClosedAt);

/// <summary>
/// Picks the most recently Closed report within 25m and same category (BR-REP-034).
/// </summary>
public static class ViolationRecurrencePrimarySelector
{
    public const double DefaultRadiusMeters = GeoMath.ProximityMatchRadiusMeters;
    public static readonly TimeSpan LookbackWindow = TimeSpan.FromDays(30);

    /// <summary>Cleanup underway at the same spot — suppresses recurrence (BR-REP-034).</summary>
    public static bool BlocksRecurrenceDetection(ReportStatus status) =>
        status is ReportStatus.Verified or ReportStatus.InProgress or ReportStatus.Reopened;

    public static Guid? SelectPrimary(
        decimal reportLatitude,
        decimal reportLongitude,
        string? reportWardCode,
        string? reportProvinceCode,
        IEnumerable<ViolationRecurrenceNearbyReport> nearby,
        double radiusMeters = DefaultRadiusMeters)
    {
        if (!AdministrativeUnitMatch.HasWardAndProvince(reportWardCode, reportProvinceCode))
            return null;

        var match = nearby
            .Where(c => AdministrativeUnitMatch.SameWardAndProvince(
                reportWardCode, reportProvinceCode, c.WardCode, c.ProvinceCode))
            .Where(c => GeoMath.HaversineMeters(reportLatitude, reportLongitude, c.Latitude, c.Longitude) <= radiusMeters)
            .OrderByDescending(c => c.ClosedAt)
            .FirstOrDefault();

        return match.Id == Guid.Empty ? null : match.Id;
    }
}
