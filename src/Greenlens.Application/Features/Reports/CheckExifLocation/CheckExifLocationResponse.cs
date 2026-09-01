namespace Greenlens.Application.Features.Reports.CheckExifLocation;

/// <summary>Pre-submit EXIF GPS comparison for citizen location-change dialog.</summary>
public sealed record CheckExifLocationResponse(
    bool HasExifGps,
    decimal? ExifLatitude,
    decimal? ExifLongitude,
    decimal SelectedLatitude,
    decimal SelectedLongitude,
    double? DistanceMeters,
    int ThresholdMeters,
    bool ShouldWarn);
