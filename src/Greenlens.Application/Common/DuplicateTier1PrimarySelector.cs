using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common;

/// <summary>Tier 1 nearby report snapshot used to pick the canonical primary (BR-REP-030).</summary>
public readonly record struct DuplicateNearbyReport(
    Guid Id,
    decimal Latitude,
    decimal Longitude,
    string? WardCode,
    string? ProvinceCode,
    ReportStatus Status,
    DateTime CreatedAt);

/// <summary>
/// Resolves which existing report is the canonical primary for a new possible duplicate.
/// Every flagged duplicate in a cluster should point to the same root so Tier 2 AI compares
/// each new report's image against that primary individually.
/// </summary>
public static class DuplicateTier1PrimarySelector
{
    public const string Tier1Source = DuplicateDetectionSources.Tier1;
    public const double DefaultRadiusMeters = GeoMath.ProximityMatchRadiusMeters;

    /// <summary>
    /// Closed reports (incl. auto-close BR-REP-016) are no longer duplicate anchors.
    /// </summary>
    public static bool IsEligibleCandidateStatus(ReportStatus status) =>
        status is not (ReportStatus.Duplicate or ReportStatus.Rejected or ReportStatus.Closed);

    /// <summary>
    /// Prefer Verified/InProgress reports (LEO đã xác minh báo cáo gốc), then oldest CreatedAt.
    /// </summary>
    public static Guid? SelectPrimary(
        decimal reportLatitude,
        decimal reportLongitude,
        string? reportWardCode,
        string? reportProvinceCode,
        IEnumerable<DuplicateNearbyReport> nearby,
        double radiusMeters = DefaultRadiusMeters)
    {
        if (!AdministrativeUnitMatch.HasWardAndProvince(reportWardCode, reportProvinceCode))
            return null;

        var match = nearby
            .Where(c => IsEligibleCandidateStatus(c.Status))
            .Where(c => AdministrativeUnitMatch.SameWardAndProvince(
                reportWardCode, reportProvinceCode, c.WardCode, c.ProvinceCode))
            .Where(c => GeoMath.HaversineMeters(reportLatitude, reportLongitude, c.Latitude, c.Longitude) <= radiusMeters)
            .OrderByDescending(c => c.Status is ReportStatus.Verified or ReportStatus.InProgress or ReportStatus.Reopened)
            .ThenBy(c => c.CreatedAt)
            .FirstOrDefault();

        return match.Id == Guid.Empty ? null : match.Id;
    }
}
