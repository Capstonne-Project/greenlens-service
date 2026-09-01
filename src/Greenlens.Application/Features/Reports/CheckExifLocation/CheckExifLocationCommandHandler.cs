using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Reports;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.CheckExifLocation;

/// <summary>
/// Pre-submit EXIF GPS check so mobile can warn when map pin differs from photo location.
/// </summary>
/// <remarks>
/// Implements: BR-REP-003 (Vietnam GPS bounds — validator), BR-REP-011 (EXIF GPS quality check).
/// Informational only — does not create a report or block submission.
/// </remarks>
public sealed class CheckExifLocationCommandHandler(
    ITempImageStore tempStore,
    IFileStorageService fileStorage,
    IImageExifAnalyzer exifAnalyzer,
    ISystemSettingsProvider systemSettings)
    : IRequestHandler<CheckExifLocationCommand, Result<CheckExifLocationResponse>>
{
    public async Task<Result<CheckExifLocationResponse>> Handle(
        CheckExifLocationCommand request,
        CancellationToken cancellationToken)
    {
        var imageResult = await ResolveImageBytesAsync(request, cancellationToken).ConfigureAwait(false);
        if (!imageResult.IsSuccess)
            return imageResult.Error!;

        var thresholdMeters = ModuleSystemSettings.ExifGpsMismatchMeters(systemSettings);
        var exif = exifAnalyzer.Analyze(imageResult.Value!, request.Latitude, request.Longitude);

        var hasExifGps = exif.Latitude.HasValue && exif.Longitude.HasValue;
        double? distanceMeters = null;
        var shouldWarn = false;

        if (hasExifGps)
        {
            distanceMeters = GeoMath.HaversineMeters(
                request.Latitude,
                request.Longitude,
                exif.Latitude!.Value,
                exif.Longitude!.Value);

            shouldWarn = ExifSuspicionEvaluator.IsGpsMismatch(
                request.Latitude,
                request.Longitude,
                exif.Latitude.Value,
                exif.Longitude.Value,
                thresholdMeters);
        }

        return new CheckExifLocationResponse(
            hasExifGps,
            exif.Latitude,
            exif.Longitude,
            request.Latitude,
            request.Longitude,
            distanceMeters,
            thresholdMeters,
            shouldWarn);
    }

    private async Task<Result<byte[]>> ResolveImageBytesAsync(
        CheckExifLocationCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.TempImageId))
        {
            var tempEntry = await tempStore.GetAsync(request.TempImageId, cancellationToken)
                .ConfigureAwait(false);

            return tempEntry is null
                ? Errors.Ai.TempImageNotFound
                : tempEntry.Bytes;
        }

        if (!fileStorage.IsOwnedPublicUrl(request.PublicUrl!, request.Key!))
            return Errors.Media.InvalidStorageUrl;

        var maxImageSizeBytes = ReportSystemSettings.MaxImageSizeBytes(systemSettings);
        var stored = await fileStorage.DownloadAsync(
                request.Key!,
                maxImageSizeBytes,
                cancellationToken)
            .ConfigureAwait(false);

        if (stored is null)
            return Errors.Media.UploadNotFound;

        if (stored.SizeBytes != request.SizeBytes)
            return Errors.Media.UploadMetadataMismatch;

        return stored.Bytes;
    }
}
