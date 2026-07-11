namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Computes distance between two GPS coordinates using PostGIS.
/// BR-CLN-002, BR-INS-004: check-in distance ≤ 200m.
/// </summary>
public interface IGeoDistanceService
{
    /// <summary>
    /// Returns the distance in meters between two points using PostGIS geography (WGS84).
    /// Uses ST_Distance(ST_MakePoint::geography, ST_MakePoint::geography).
    /// </summary>
    Task<double> GetDistanceInMetersAsync(
        decimal lat1, decimal lng1,
        decimal lat2, decimal lng2,
        CancellationToken ct = default);
}
