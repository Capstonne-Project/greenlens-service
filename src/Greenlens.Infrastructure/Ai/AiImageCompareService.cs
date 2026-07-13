using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Infrastructure.Ai;

/// <summary>
/// HTTP adapter for the Python/FastAPI AI image-compare endpoint (DINOv2).
/// </summary>
/// <remarks>
/// Implements: BR-AI-002 (image similarity for duplicate detection),
/// BR-AI-006 (5s timeout → return null so caller falls back to Tier 1 geo_time).
/// Endpoint: POST /api/v1/compare-images  body: { image_url_a, image_url_b }.
/// Reuses the named "AiService" HttpClient (same BaseUrl as classify/moderation).
/// </remarks>
internal sealed class AiImageCompareService(
    IHttpClientFactory httpClientFactory,
    IOptions<AiOptions> options,
    ILogger<AiImageCompareService> logger)
    : IAiImageCompareService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ImageCompareResult?> CompareAsync(
        string imageUrlA,
        string imageUrlB,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrlA) || string.IsNullOrWhiteSpace(imageUrlB))
            return null;

        var client = httpClientFactory.CreateClient("AiService");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));

        var payload = new { image_url_a = imageUrlA, image_url_b = imageUrlB };

        try
        {
            using var response = await client
                .PostAsJsonAsync("/api/v1/compare-images", payload, cts.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("AI compare-images returned {StatusCode}", (int)response.StatusCode);
                return null; // Tier 1 fallback
            }

            var dto = await response.Content
                .ReadFromJsonAsync<CompareImagesResponseDto>(JsonOptions, cts.Token)
                .ConfigureAwait(false);

            if (dto is null)
                return null;

            return new ImageCompareResult(dto.Similarity, dto.IsSameScene, dto.Model, dto.ProcessingTimeMs);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // BR-AI-006: our own 5s timeout, not a caller abort.
            logger.LogWarning("AI compare-images timed out after {Seconds}s", options.Value.TimeoutSeconds);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "AI compare-images network error");
            return null;
        }
    }

    private sealed class CompareImagesResponseDto
    {
        [JsonPropertyName("similarity")] public decimal Similarity { get; init; }
        [JsonPropertyName("is_same_scene")] public bool IsSameScene { get; init; }
        [JsonPropertyName("model")] public string Model { get; init; } = "";
        [JsonPropertyName("processing_time_ms")] public int ProcessingTimeMs { get; init; }
    }
}
