namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Contract for calling the AI Service to classify and moderate a pollution report image.
/// </summary>
/// <remarks>
/// Implements: BR-AI-001 (image classification), BR-AI-006 (timeout 5s → tag ai_pending),
/// BR-AI-007 (EXIF stripping handled by the implementation before forwarding).
/// </remarks>
public interface IAiClassificationService
{
    /// <summary>
    /// Forward an image stream to the AI Service and return the classification result.
    /// Returns null when the AI Service is unavailable (timeout / 5xx) — caller should tag report as ai_pending.
    /// </summary>
    Task<AiClassificationResult?> ClassifyAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken ct = default);
}

// ── Result DTOs ─────────────────────────────────────────────────────────────

public sealed record AiClassificationResult(
    AiDecision Decision,
    string Reason,
    AiClassifyDetail Classify);

public sealed record AiClassifyDetail(
    string? PrimaryClass,
    double Confidence,
    string Severity,
    string ImageRelevance,
    double PollutionCoverageRatio,
    IReadOnlyList<AiPredictionItem> Predictions,
    double InferenceTimeMs,
    bool YoloActive,
    bool SceneClassifierActive,
    string? ModelVersion,
    bool NoiseSupported);

public sealed record AiPredictionItem(
    string Class,
    double Confidence,
    int BboxCount);

public enum AiDecision
{
    AcceptableReportImage,
    NeedManualReview,
    IrrelevantOrSuspectedAbusive
}
