namespace Greenlens.Application.Common.Map;

/// <summary>
/// Public map coordinate rounding (BR-MAP-004 — ~11 m precision at 4 decimals).
/// </summary>
public static class PublicMapCoordinateRounding
{
    public static decimal RoundLatitude(decimal latitude, int decimalPlaces = 4) =>
        Math.Round(latitude, decimalPlaces, MidpointRounding.AwayFromZero);

    public static decimal RoundLongitude(decimal longitude, int decimalPlaces = 4) =>
        Math.Round(longitude, decimalPlaces, MidpointRounding.AwayFromZero);
}
