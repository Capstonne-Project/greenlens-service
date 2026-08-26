using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Infrastructure.Ai;

/// <summary>
/// HTTP adapter for the Python/FastAPI AI Service.
/// </summary>
/// <remarks>
/// Implements: BR-AI-001 (classification), BR-AI-006 (timeout → return null).
/// Endpoint: POST /api/v1/classify-moderation-upload  field: "image".
/// </remarks>
internal sealed class AiClassificationService(
    IHttpClientFactory httpClientFactory,
    IOptions<AiOptions> options,
    ISystemSettingsProvider systemSettings,
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
        var opts = options.Value;
        var baseUrl = opts.BaseUrl.TrimEnd('/');
        var timeoutSec = ModuleSystemSettings.Ai(systemSettings).TimeoutSeconds;
        var endpoint = $"{baseUrl}/api/v1/classify-moderation-upload";

        long? streamLength = imageStream.CanSeek ? imageStream.Length : null;

        logger.LogInformation(
            "[AI-DIAG] Classify START → {Endpoint} | file={FileName} contentType={ContentType} sizeBytes={SizeBytes} timeoutSec={TimeoutSec}",
            endpoint,
            fileName,
            contentType,
            streamLength,
            timeoutSec);

        using var client = httpClientFactory.CreateClient("AiService");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "image", fileName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await client
                .PostAsync("/api/v1/classify-moderation-upload", content, cts.Token)
                .ConfigureAwait(false);

            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var bodyPreview = await SafeReadBodyPreviewAsync(response, cts.Token).ConfigureAwait(false);
                logger.LogWarning(
                    "[AI-DIAG] Classify FAIL HTTP {StatusCode} in {ElapsedMs}ms | file={FileName} body={BodyPreview}",
                    (int)response.StatusCode,
                    sw.ElapsedMilliseconds,
                    fileName,
                    bodyPreview);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            var raw = JsonSerializer.Deserialize<AiRawResponse>(json, JsonOptions);
            if (raw is null)
            {
                logger.LogWarning(
                    "[AI-DIAG] Classify FAIL deserialize null in {ElapsedMs}ms | file={FileName} jsonLen={JsonLen}",
                    sw.ElapsedMilliseconds,
                    fileName,
                    json.Length);
                return null;
            }

            var mapped = MapToResult(raw);
            logger.LogInformation(
                "[AI-DIAG] Classify OK in {ElapsedMs}ms | file={FileName} decision={Decision} primary={Primary} conf={Confidence:F3} severity={Severity} relevance={Relevance} yolo={Yolo} scene={Scene} aiInferenceMs={AiInferenceMs} model={ModelVersion}",
                sw.ElapsedMilliseconds,
                fileName,
                mapped.Decision,
                mapped.Classify.PrimaryClass,
                mapped.Classify.Confidence,
                mapped.Classify.Severity,
                mapped.Classify.ImageRelevance,
                mapped.Classify.YoloActive,
                mapped.Classify.SceneClassifierActive,
                mapped.Classify.InferenceTimeMs,
                mapped.Classify.ModelVersion);

            return mapped;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            // BR-AI-006: fail fast so the optional pre-submit AI flow can fall back to manual input.
            logger.LogWarning(
                "[AI-DIAG] Classify TIMEOUT after {TimeoutSec}s (elapsed {ElapsedMs}ms) | endpoint={Endpoint} file={FileName} — tăng Ai:TimeoutSeconds nếu YOLO cold-start > timeout",
                timeoutSec,
                sw.ElapsedMilliseconds,
                endpoint,
                fileName);
            return null;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            logger.LogError(
                ex,
                "[AI-DIAG] Classify CONNECTION FAIL in {ElapsedMs}ms | endpoint={Endpoint} file={FileName} — kiểm tra AI uvicorn :8000 và Ai:BaseUrl",
                sw.ElapsedMilliseconds,
                endpoint,
                fileName);
            return null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(
                ex,
                "[AI-DIAG] Classify EXCEPTION in {ElapsedMs}ms | endpoint={Endpoint} file={FileName}",
                sw.ElapsedMilliseconds,
                endpoint,
                fileName);
            return null;
        }
    }

    private static async Task<string> SafeReadBodyPreviewAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(text))
                return "(empty)";
            return text.Length <= 300 ? text : text[..300] + "…";
        }
        catch
        {
            return "(unreadable)";
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
            .Select(p => new AiPredictionItem(
                p.Class ?? string.Empty,
                p.Confidence,
                p.BboxCount,
                p.Subtypes?
                    .Select(s => new AiTrashSubtypeItem(s.Subtype ?? string.Empty, s.Count, s.Confidence))
                    .ToArray(),
                p.Boxes?
                    .Select(b => new AiBoxItem(b.X1, b.Y1, b.X2, b.Y2, b.Confidence, b.Subtype, b.SubtypeConfidence))
                    .ToArray()))
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
        [JsonPropertyName("subtypes")] public List<AiRawSubtype>? Subtypes { get; init; }
        [JsonPropertyName("boxes")] public List<AiRawBox>? Boxes { get; init; }
    }

    private sealed class AiRawBox
    {
        [JsonPropertyName("x1")] public double X1 { get; init; }
        [JsonPropertyName("y1")] public double Y1 { get; init; }
        [JsonPropertyName("x2")] public double X2 { get; init; }
        [JsonPropertyName("y2")] public double Y2 { get; init; }
        [JsonPropertyName("confidence")] public double Confidence { get; init; }
        [JsonPropertyName("subtype")] public string? Subtype { get; init; }
        [JsonPropertyName("subtype_confidence")] public double? SubtypeConfidence { get; init; }
    }

    private sealed class AiRawSubtype
    {
        [JsonPropertyName("subtype")] public string? Subtype { get; init; }
        [JsonPropertyName("count")] public int Count { get; init; }
        [JsonPropertyName("confidence")] public double Confidence { get; init; }
    }
}
