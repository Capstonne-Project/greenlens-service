using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;

namespace Greenlens.Application.Features.Reports.Common;

/// <summary>
/// BR-CLN-004: validate progress updates were captured near the report site.
/// Mobile sends latitude/longitude from photo EXIF (when available), not device GPS.
/// Uses <see cref="ModuleSystemSettings.ProgressUpdateMaxDistanceMeters"/>.
/// </summary>
internal static class ProgressUpdateExifGuard
{
    /// <summary>
    /// Validates submitted coordinates against the site, and re-checks EXIF embedded in R2 images when present.
    /// </summary>
    public static async Task<Error?> ValidateAsync(
        decimal submittedLatitude,
        decimal submittedLongitude,
        decimal siteLatitude,
        decimal siteLongitude,
        IReadOnlyList<string> imageUrls,
        IFileStorageService fileStorage,
        IImageExifAnalyzer exifAnalyzer,
        IGeoDistanceService geoDistance,
        ISystemSettingsProvider systemSettings,
        CancellationToken ct)
    {
        var maxDistanceMeters = ModuleSystemSettings.ProgressUpdateMaxDistanceMeters(systemSettings);

        var submittedDistance = await geoDistance.GetDistanceInMetersAsync(
                submittedLatitude,
                submittedLongitude,
                siteLatitude,
                siteLongitude,
                ct)
            .ConfigureAwait(false);

        if (submittedDistance > maxDistanceMeters)
        {
            return imageUrls.Count > 0
                ? Errors.Progress.PhotoTooFarFromSite(submittedDistance)
                : Errors.Progress.TooFarFromSite(submittedDistance);
        }

        if (imageUrls.Count == 0)
            return null;

        return await ValidateProgressImageFileExifAsync(
            imageUrls,
            siteLatitude,
            siteLongitude,
            maxDistanceMeters,
            fileStorage,
            exifAnalyzer,
            systemSettings,
            ct).ConfigureAwait(false);
    }

    private static async Task<Error?> ValidateProgressImageFileExifAsync(
        IReadOnlyList<string> imageUrls,
        decimal siteLatitude,
        decimal siteLongitude,
        int maxDistanceMeters,
        IFileStorageService fileStorage,
        IImageExifAnalyzer exifAnalyzer,
        ISystemSettingsProvider systemSettings,
        CancellationToken ct)
    {
        var maxImageSizeBytes = ReportSystemSettings.MaxImageSizeBytes(systemSettings);

        foreach (var rawUrl in imageUrls)
        {
            var url = rawUrl.Trim();
            var key = fileStorage.TryGetKeyFromOwnedPublicUrl(url);
            if (key is null)
                return Errors.Media.InvalidStorageUrl;

            var stored = await fileStorage.DownloadAsync(key, maxImageSizeBytes, ct).ConfigureAwait(false);
            if (stored is null)
                return Errors.Media.UploadNotFound;

            var exif = exifAnalyzer.Analyze(stored.Bytes, siteLatitude, siteLongitude);
            if (!exif.Latitude.HasValue || !exif.Longitude.HasValue)
                continue;

            var distanceMeters = GeoMath.HaversineMeters(
                siteLatitude,
                siteLongitude,
                exif.Latitude.Value,
                exif.Longitude.Value);

            if (distanceMeters > maxDistanceMeters)
                return Errors.Progress.PhotoTooFarFromSite(distanceMeters);
        }

        return null;
    }
}
