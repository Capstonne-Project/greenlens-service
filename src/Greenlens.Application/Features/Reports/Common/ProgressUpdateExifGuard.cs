using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;

namespace Greenlens.Application.Features.Reports.Common;

/// <summary>
/// BR-CLN-004: validate progress photos were captured near the report site using EXIF GPS.
/// Uses <see cref="ModuleSystemSettings.ProgressUpdateMaxDistanceMeters"/>, not citizen EXIF mismatch threshold.
/// </summary>
internal static class ProgressUpdateExifGuard
{
    public static async Task<Error?> ValidateProgressImageUrlsAsync(
        IReadOnlyList<string> imageUrls,
        decimal siteLatitude,
        decimal siteLongitude,
        IFileStorageService fileStorage,
        IImageExifAnalyzer exifAnalyzer,
        ISystemSettingsProvider systemSettings,
        CancellationToken ct)
    {
        if (imageUrls.Count == 0)
            return null;

        var maxDistanceMeters = ModuleSystemSettings.ProgressUpdateMaxDistanceMeters(systemSettings);
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
