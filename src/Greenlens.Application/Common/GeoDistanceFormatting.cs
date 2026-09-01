namespace Greenlens.Application.Common;

internal static class GeoDistanceFormatting
{
    public static string Format(double meters) =>
        meters >= 1000
            ? $"{meters / 1000.0:F1} km"
            : $"{meters:F0} m";
}
