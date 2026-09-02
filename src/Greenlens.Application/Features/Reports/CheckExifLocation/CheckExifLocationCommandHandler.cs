using System.Diagnostics;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Reports;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

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
    ISystemSettingsProvider systemSettings,
    ILogger<CheckExifLocationCommandHandler> logger)
    : IRequestHandler<CheckExifLocationCommand, Result<CheckExifLocationResponse>>
{
    public async Task<Result<CheckExifLocationResponse>> Handle(
        CheckExifLocationCommand request,
        CancellationToken cancellationToken)
    {
        var totalSw = Stopwatch.StartNew();

        logger.LogInformation(
            "[EXIF-CHECK] start | lat={Latitude} lng={Longitude} tempImageId={HasTemp} key={Key} sizeBytes={SizeBytes}",
            request.Latitude,
            request.Longitude,
            !string.IsNullOrWhiteSpace(request.TempImageId),
            request.Key,
            request.SizeBytes);

        var imageResult = await ResolveImageBytesAsync(request, cancellationToken).ConfigureAwait(false);
        if (!imageResult.IsSuccess)
        {
            logger.LogWarning(
                "[EXIF-CHECK] image resolve failed in {ElapsedMs}ms | code={ErrorCode}",
                totalSw.ElapsedMilliseconds,
                imageResult.Error!.Code);
            return imageResult.Error!;
        }

        var thresholdMeters = ModuleSystemSettings.ExifGpsMismatchMeters(systemSettings);

        var exifSw = Stopwatch.StartNew();
        var exif = exifAnalyzer.Analyze(imageResult.Value!, request.Latitude, request.Longitude);
        exifSw.Stop();

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

        logger.LogInformation(
            "[EXIF-CHECK] done in {TotalMs}ms (exifAnalyze={ExifMs}ms) | hasExifGps={HasExifGps} distanceM={Distance} thresholdM={Threshold} shouldWarn={ShouldWarn}",
            totalSw.ElapsedMilliseconds,
            exifSw.ElapsedMilliseconds,
            hasExifGps,
            distanceMeters,
            thresholdMeters,
            shouldWarn);

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
        {
            logger.LogWarning(
                "[EXIF-CHECK] invalid storage url | key={Key} url={Url}",
                request.Key,
                request.PublicUrl);
            return Errors.Media.InvalidStorageUrl;
        }

        var maxImageSizeBytes = ReportSystemSettings.MaxImageSizeBytes(systemSettings);

        var downloadSw = Stopwatch.StartNew();
        var stored = await fileStorage.DownloadAsync(
                request.Key!,
                maxImageSizeBytes,
                cancellationToken)
            .ConfigureAwait(false);
        downloadSw.Stop();

        if (stored is null)
        {
            logger.LogWarning(
                "[EXIF-CHECK] R2 download returned null in {DownloadMs}ms | key={Key}",
                downloadSw.ElapsedMilliseconds,
                request.Key);
            return Errors.Media.UploadNotFound;
        }

        logger.LogInformation(
            "[EXIF-CHECK] R2 download OK in {DownloadMs}ms | key={Key} bytes={Bytes}",
            downloadSw.ElapsedMilliseconds,
            request.Key,
            stored.SizeBytes);

        if (stored.SizeBytes != request.SizeBytes)
        {
            logger.LogWarning(
                "[EXIF-CHECK] size mismatch | key={Key} claimed={Claimed} actual={Actual}",
                request.Key,
                request.SizeBytes,
                stored.SizeBytes);
            return Errors.Media.UploadMetadataMismatch;
        }

        return stored.Bytes;
    }
}
