using System.Text.Json;
using System.Text.Json.Serialization;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Infrastructure.Ai;

/// <summary>
/// HTTP adapter for the Python/FastAPI AI Service.
/// </summary>
/// <remarks>
/// Implements: BR-AI-001 (classification), BR-AI-006 (timeout 5s → return null for ai_pending fallback).
/// Endpoint: POST /api/v1/classify-moderation-upload  field: "image".
/// </remarks>
internal sealed class AiClassificationService(
    IHttpClientFactory httpClientFactory,
    IOptions<AiOptions> options,
    ILogger<AiClassificationService> logger)
    : IAiClassificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public async Task<AiClassificationResult?> ClassifyAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient("AiService");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "image", fileName);

        try
        {
            var response = await client
                .PostAsync("/api/v1/classify-moderation-upload", content, cts.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("AI Service returned {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            var raw = JsonSerializer.Deserialize<AiRawResponse>(json, JsonOptions);
            if (raw is null) return null;

            return MapToResult(raw);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // BR-AI-006: timeout — caller will tag ai_pending
            logger.LogWarning("AI Service timed out after {Seconds}s", options.Value.TimeoutSeconds);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI Service call failed");
            return null;
        }
    }

    private static AiClassificationResult MapToResult(AiRawResponse raw)
    {
        var decision = raw.Decision switch
        {
            "ACCEPTABLE_REPORT_IMAGE" => AiDecision.AcceptableReportImage,
            "NEED_MANUAL_REVIEW" => AiDecision.NeedManualReview,
            _ => AiDecision.IrrelevantOrSuspectedAbusive
        };

        var predictions = raw.Classify?.Predictions?
            .Select(p => new AiPredictionItem(p.Class ?? string.Empty, p.Confidence, p.BboxCount))
            .ToArray() ?? [];

        var classify = new AiClassifyDetail(
            raw.Classify?.PrimaryClass,
            raw.Classify?.Confidence ?? 0,
            raw.Classify?.Severity ?? "MEDIUM",
            raw.Classify?.ImageRelevance ?? string.Empty,
            raw.Classify?.PollutionCoverageRatio ?? 0,
            predictions,
            raw.Classify?.InferenceTimeMs ?? 0,
            raw.Classify?.YoloActive ?? false,
            raw.Classify?.SceneClassifierActive ?? false,
            raw.Classify?.ModelVersion,
            raw.Classify?.NoiseSupported ?? false);

        return new AiClassificationResult(decision, raw.Reason ?? string.Empty, classify);
    }

    // ── Raw JSON deserialization models ─────────────────────────────────────

    private sealed class AiRawResponse
    {
        [JsonPropertyName("decision")] public string? Decision { get; init; }
        [JsonPropertyName("reason")] public string? Reason { get; init; }
        [JsonPropertyName("classify")] public AiRawClassify? Classify { get; init; }
    }

    private sealed class AiRawClassify
    {
        [JsonPropertyName("primary_class")] public string? PrimaryClass { get; init; }
        [JsonPropertyName("confidence")] public double Confidence { get; init; }
        [JsonPropertyName("severity")] public string? Severity { get; init; }
        [JsonPropertyName("image_relevance")] public string? ImageRelevance { get; init; }
        [JsonPropertyName("pollution_coverage_ratio")] public double PollutionCoverageRatio { get; init; }
        [JsonPropertyName("predictions")] public List<AiRawPrediction>? Predictions { get; init; }
        [JsonPropertyName("inference_time_ms")] public double InferenceTimeMs { get; init; }
        [JsonPropertyName("yolo_active")] public bool YoloActive { get; init; }
        [JsonPropertyName("scene_classifier_active")] public bool SceneClassifierActive { get; init; }
        [JsonPropertyName("model_version")] public string? ModelVersion { get; init; }
        [JsonPropertyName("noise_supported")] public bool NoiseSupported { get; init; }
    }

    private sealed class AiRawPrediction
    {
        [JsonPropertyName("class")] public string? Class { get; init; }
        [JsonPropertyName("confidence")] public double Confidence { get; init; }
        [JsonPropertyName("bbox_count")] public int BboxCount { get; init; }
    }
}
