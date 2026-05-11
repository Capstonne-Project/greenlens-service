namespace Greenlens.Application.Common.Map;

/// <summary>
/// Public map coordinate rounding (BR-MAP-004 — ~11 m precision at 4 decimals).
/// </summary>
public static class PublicMapCoordinateRounding
{
    private const int DecimalPlaces = 4;

    public static decimal RoundLatitude(decimal latitude) =>
        Math.Round(latitude, DecimalPlaces, MidpointRounding.AwayFromZero);

    public static decimal RoundLongitude(decimal longitude) =>
        Math.Round(longitude, DecimalPlaces, MidpointRounding.AwayFromZero);
}
