using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Media.UploadReportVideo;

/// <summary>
/// Upload report video to R2 Cloudflare with server-side transcoding.
/// Compression profile: H.264 720p CRF 28, AAC 96k, faststart — similar to Messenger/Zalo.
/// </summary>
/// <remarks>
/// Implements: BR-REP-002 (video 1, mp4/mov, server-side compression).
/// Flow: validate → transcode via IVideoTranscoder → upload compressed to R2 → return URL.
/// </remarks>
public sealed class UploadReportVideoCommandHandler(
    IVideoTranscoder videoTranscoder,
    IFileStorageService fileStorage,
    ILogger<UploadReportVideoCommandHandler> logger)
    : IRequestHandler<UploadReportVideoCommand, Result<UploadReportVideoResponse>>
{
    private const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100MB — user-approved limit

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/quicktime" // .mov files
    };

    public async Task<Result<UploadReportVideoResponse>> Handle(
        UploadReportVideoCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Input validation ──
        if (!AllowedContentTypes.Contains(request.ContentType))
            return Errors.Media.InvalidVideoType;

        if (request.FileSize > MaxFileSizeBytes)
            return Errors.Media.VideoTooLarge;

        // ── 2. Transcode (compress) ──
        VideoTranscodeResult transcodeResult;
        try
        {
            transcodeResult = await videoTranscoder.TranscodeAsync(
                request.FileStream,
                request.FileName,
                new VideoTranscodeOptions
                {
                    MaxWidth = 1280,
                    MaxHeight = 720,
                    Crf = 28,
                    Preset = "fast",
                    AudioBitrateKbps = 96,
                    MaxDurationSeconds = 60
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (VideoDurationExceededException ex)
        {
            logger.LogWarning("Video duration {Actual}s exceeds max {Max}s for {File}",
                ex.ActualSeconds, ex.MaxSeconds, request.FileName);
            return Errors.Media.VideoDurationExceeded;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to transcode video {FileName}", request.FileName);
            return Errors.Media.VideoTranscodeFailed;
        }

        // ── 3. Upload compressed video to R2 ──
        using (transcodeResult)
        {
            FileUploadResult uploadResult;
            try
            {
                // Output is always .mp4 after transcode
                var outputFileName = Path.ChangeExtension(request.FileName, ".mp4");

                uploadResult = await fileStorage.UploadAsync(
                    transcodeResult.OutputStream,
                    outputFileName,
                    transcodeResult.OutputContentType,
                    "reports/videos",
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to upload transcoded video to R2");
                return Errors.Users.StorageUploadFailed;
            }

            logger.LogInformation(
                "Video uploaded: {Key} — {OriginalMB:F1}MB → {CompressedMB:F1}MB ({Ratio:P0} reduction)",
                uploadResult.Key,
                request.FileSize / 1_048_576.0,
                transcodeResult.CompressedSizeBytes / 1_048_576.0,
                1.0 - (double)transcodeResult.CompressedSizeBytes / request.FileSize);

            return new UploadReportVideoResponse(
                uploadResult.Url,
                uploadResult.Key,
                "Tải video báo cáo thành công.",
                transcodeResult.OutputContentType,
                request.FileSize,
                transcodeResult.CompressedSizeBytes,
                transcodeResult.DurationSeconds,
                transcodeResult.Width,
                transcodeResult.Height);
        }
    }
}
