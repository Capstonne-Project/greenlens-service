namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Transcodes a video stream (compress, resize, re-encode) and returns the result.
/// Infrastructure implements this with FFmpeg.
/// </summary>
public interface IVideoTranscoder
{
    /// <summary>
    /// Transcode the input video stream, compressing it according to the given options.
    /// Returns a result containing the compressed stream, metadata, and output file path.
    /// </summary>
    /// <remarks>
    /// The caller is responsible for disposing the <see cref="VideoTranscodeResult.OutputStream"/>.
    /// Temp files (if any) are cleaned up when the result is disposed.
    /// </remarks>
    Task<VideoTranscodeResult> TranscodeAsync(
        Stream inputStream,
        string inputFileName,
        VideoTranscodeOptions options,
        CancellationToken ct = default);
}

/// <summary>
/// Options controlling video transcode output quality and limits.
/// </summary>
public sealed record VideoTranscodeOptions
{
    /// <summary>Max output width in pixels. Videos wider than this are scaled down, keeping aspect ratio.</summary>
    public int MaxWidth { get; init; } = 1280;

    /// <summary>Max output height in pixels.</summary>
    public int MaxHeight { get; init; } = 720;

    /// <summary>H.264 Constant Rate Factor (0-51). Lower = better quality, larger file. 28 = "good enough".</summary>
    public int Crf { get; init; } = 28;

    /// <summary>FFmpeg encoding preset. "fast" balances speed and compression.</summary>
    public string Preset { get; init; } = "fast";

    /// <summary>Audio bitrate in kbps. 96 is sufficient for ambient audio.</summary>
    public int AudioBitrateKbps { get; init; } = 96;

    /// <summary>Max allowed duration in seconds. Videos longer are rejected.</summary>
    public int MaxDurationSeconds { get; init; } = 60;
}

/// <summary>
/// Result of a video transcode operation. Implements IDisposable to clean up temp files.
/// </summary>
public sealed class VideoTranscodeResult : IDisposable
{
    public required Stream OutputStream { get; init; }
    public required long CompressedSizeBytes { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int DurationSeconds { get; init; }
    public required string OutputContentType { get; init; }

    /// <summary>Temp file path to clean up on dispose (if any).</summary>
    public string? TempInputPath { get; init; }
    public string? TempOutputPath { get; init; }

    public void Dispose()
    {
        OutputStream.Dispose();
        TryDeleteFile(TempInputPath);
        TryDeleteFile(TempOutputPath);
    }

    private static void TryDeleteFile(string? path)
    {
        if (path is not null && File.Exists(path))
        {
            try { File.Delete(path); }
            catch { /* best-effort cleanup */ }
        }
    }
}

/// <summary>
/// Thrown when input video duration exceeds the configured maximum.
/// Defined in Application layer so both handler (catch) and infrastructure (throw) can reference it.
/// </summary>
public sealed class VideoDurationExceededException(int actualSeconds, int maxSeconds)
    : Exception($"Video duration {actualSeconds}s exceeds maximum {maxSeconds}s")
{
    public int ActualSeconds => actualSeconds;
    public int MaxSeconds => maxSeconds;
}

