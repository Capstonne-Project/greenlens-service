using FFMpegCore;
using FFMpegCore.Enums;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Video;

/// <summary>
/// Transcodes video using FFmpeg CLI via FFMpegCore wrapper.
/// Compression profile mirrors Messenger/Zalo: H.264, 720p cap, CRF 28, AAC, faststart.
/// </summary>
/// <remarks>Requires FFmpeg binary on PATH or configured via GlobalFFOptions.</remarks>
internal sealed class FFmpegVideoTranscoder(ILogger<FFmpegVideoTranscoder> logger) : IVideoTranscoder
{
    public async Task<VideoTranscodeResult> TranscodeAsync(
        Stream inputStream,
        string inputFileName,
        VideoTranscodeOptions options,
        CancellationToken ct = default)
    {
        // ── 1. Save input to temp file (FFmpeg needs seekable file) ──
        var tempDir = Path.Combine(Path.GetTempPath(), "greenlens-video");
        Directory.CreateDirectory(tempDir);

        var inputExt = Path.GetExtension(inputFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(inputExt)) inputExt = ".mp4";

        var tempInputPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}{inputExt}");
        var tempOutputPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}.mp4");

        await using (var fs = File.Create(tempInputPath))
        {
            await inputStream.CopyToAsync(fs, ct).ConfigureAwait(false);
        }

        try
        {
            // ── 2. Probe input metadata ──
            var mediaInfo = await FFProbe.AnalyseAsync(tempInputPath, cancellationToken: ct)
                .ConfigureAwait(false);

            var duration = mediaInfo.Duration;
            if (duration.TotalSeconds > options.MaxDurationSeconds)
            {
                // Clean up and throw — handler will catch and return error
                TryDeleteFile(tempInputPath);
                throw new VideoDurationExceededException(
                    (int)duration.TotalSeconds, options.MaxDurationSeconds);
            }

            var videoStream = mediaInfo.PrimaryVideoStream;
            var srcWidth = videoStream?.Width ?? 1920;
            var srcHeight = videoStream?.Height ?? 1080;

            // ── 3. Calculate output dimensions (cap at 720p, keep aspect ratio) ──
            var (outWidth, outHeight) = CalculateScaledDimensions(
                srcWidth, srcHeight, options.MaxWidth, options.MaxHeight);

            logger.LogInformation(
                "Transcoding video: {Input} ({SrcW}x{SrcH}, {Duration}s) → {OutW}x{OutH}, CRF {Crf}",
                inputFileName, srcWidth, srcHeight, (int)duration.TotalSeconds,
                outWidth, outHeight, options.Crf);

            // ── 4. Transcode with FFmpeg ──
            await FFMpegArguments
                .FromFileInput(tempInputPath)
                .OutputToFile(tempOutputPath, overwrite: true, opts => opts
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithConstantRateFactor(options.Crf)
                    .WithAudioCodec(AudioCodec.Aac)
                    .WithAudioBitrate(options.AudioBitrateKbps)
                    .WithVideoFilters(f => f.Scale(outWidth, outHeight))
                    .WithCustomArgument("-preset " + options.Preset)
                    .WithCustomArgument("-profile:v high")
                    .WithCustomArgument("-level 4.1")
                    .WithCustomArgument("-pix_fmt yuv420p")
                    .WithFastStart())
                .CancellableThrough(ct)
                .ProcessAsynchronously()
                .ConfigureAwait(false);

            // ── 5. Build result ──
            var outputInfo = new FileInfo(tempOutputPath);
            var outputStream = File.OpenRead(tempOutputPath);

            logger.LogInformation(
                "Transcode complete: {OriginalSize}→{CompressedSize} bytes ({Ratio:P0} reduction)",
                new FileInfo(tempInputPath).Length,
                outputInfo.Length,
                1.0 - (double)outputInfo.Length / new FileInfo(tempInputPath).Length);

            return new VideoTranscodeResult
            {
                OutputStream = outputStream,
                CompressedSizeBytes = outputInfo.Length,
                Width = outWidth,
                Height = outHeight,
                DurationSeconds = (int)duration.TotalSeconds,
                OutputContentType = "video/mp4",
                TempInputPath = tempInputPath,
                TempOutputPath = tempOutputPath
            };
        }
        catch (VideoDurationExceededException)
        {
            throw; // re-throw domain exception
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FFmpeg transcode failed for {FileName}", inputFileName);
            TryDeleteFile(tempInputPath);
            TryDeleteFile(tempOutputPath);
            throw;
        }
    }

    /// <summary>
    /// Scale down to fit within maxWidth×maxHeight while keeping aspect ratio.
    /// If already smaller, return original dimensions.
    /// Ensures both dimensions are divisible by 2 (codec requirement).
    /// </summary>
    private static (int Width, int Height) CalculateScaledDimensions(
        int srcWidth, int srcHeight, int maxWidth, int maxHeight)
    {
        if (srcWidth <= maxWidth && srcHeight <= maxHeight)
        {
            // Already within bounds — just ensure divisible by 2
            return (srcWidth - srcWidth % 2, srcHeight - srcHeight % 2);
        }

        var widthRatio = (double)maxWidth / srcWidth;
        var heightRatio = (double)maxHeight / srcHeight;
        var ratio = Math.Min(widthRatio, heightRatio);

        var newWidth = (int)(srcWidth * ratio);
        var newHeight = (int)(srcHeight * ratio);

        // Ensure divisible by 2
        newWidth -= newWidth % 2;
        newHeight -= newHeight % 2;

        return (newWidth, newHeight);
    }

    private static void TryDeleteFile(string? path)
    {
        if (path is not null && File.Exists(path))
        {
            try { File.Delete(path); }
            catch { /* best-effort */ }
        }
    }
}

