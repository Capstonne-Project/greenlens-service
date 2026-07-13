namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Contract for the AI image-compare endpoint (CLIP/DINOv2) used in Tier 2 duplicate detection.
/// </summary>
/// <remarks>
/// Implements: BR-AI-002 (image similarity for duplicate detection), BR-AI-006 (5s timeout → fallback Tier 1).
/// Endpoint: POST /api/v1/compare-images. The AI service downloads both images from the given URLs.
/// </remarks>
public interface IAiImageCompareService
{
    /// <summary>
    /// Compare two publicly-readable image URLs. Returns null when the AI service is
    /// unavailable / timed out / non-success — the caller falls back to Tier 1 (geo_time).
    /// </summary>
    Task<ImageCompareResult?> CompareAsync(
        string imageUrlA,
        string imageUrlB,
        CancellationToken ct = default);
}

/// <summary>Result of an AI image comparison.</summary>
public sealed record ImageCompareResult(
    decimal Similarity,
    bool IsSameScene,
    string Model,
    int ProcessingTimeMs);
