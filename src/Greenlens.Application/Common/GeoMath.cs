namespace Greenlens.Application.Common;

/// <summary>
/// In-memory geo helpers for report duplicate detection (BR-REP-030 Tier 1).
/// PostGIS-backed distance for check-in lives in <see cref="Interfaces.IGeoDistanceService"/>.
/// </summary>
public static class GeoMath
{
    /// <summary>GPS proximity for Tier 1 duplicate (BR-REP-030) and recurrence (BR-REP-034).</summary>
    public const double ProximityMatchRadiusMeters = 25.0;

    /// <summary>
    /// Great-circle distance in meters between two WGS84 points (Haversine).
    /// </summary>
    public static double HaversineMeters(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const double earthRadiusMeters = 6_371_000.0;
        var dLat = (double)(lat2 - lat1) * Math.PI / 180.0;
        var dLng = (double)(lng2 - lng1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos((double)lat1 * Math.PI / 180.0) * Math.Cos((double)lat2 * Math.PI / 180.0)
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        // FP rounding can push a slightly above 1; clamp to keep Asin/Sqrt well-defined.
        a = Math.Clamp(a, 0.0, 1.0);
        return earthRadiusMeters * 2 * Math.Asin(Math.Sqrt(a));
    }
}
