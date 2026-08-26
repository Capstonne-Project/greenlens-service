using Greenlens.Application.Features.Catalog.GetPollutionCategories;

namespace Greenlens.Application.Features.Reports.AnalyzeReportImage;

public sealed record AnalyzeReportImageResponse(
    string TempImageId,
    int ExpiresInSeconds,
    AiResultDto AiResult,
    /// <summary>Catalog row from AI primary_class — FE uses <c>id</c> + <c>nameVi</c> to auto-fill the form.</summary>
    PollutionCategoryListItemDto? SuggestedCategory,
    /// <summary>Best-effort LLM-drafted Vietnamese description — null when the LLM is unavailable/unconfigured. FE may auto-fill the description field.</summary>
    string? SuggestedDescription);

public sealed record AiResultDto(
    string Decision,
    string Reason,
    AiClassifyDto? Classify);

public sealed record AiClassifyDto(
    string? PrimaryClass,
    double Confidence,
    string Severity,
    string ImageRelevance,
    double PollutionCoverageRatio,
    IReadOnlyList<AiPredictionDto> Predictions,
    double InferenceTimeMs,
    bool YoloActive,
    bool SceneClassifierActive);

public sealed record AiPredictionDto(
    string Class,
    double Confidence,
    int BboxCount,
    IReadOnlyList<AiTrashSubtypeDto>? Subtypes,
    IReadOnlyList<AiBoxDto>? Boxes);

public sealed record AiTrashSubtypeDto(
    string Subtype,
    int Count,
    double Confidence);

/// <summary>Absolute pixel bbox (xyxy) against the original uploaded image dimensions.</summary>
public sealed record AiBoxDto(
    double X1,
    double Y1,
    double X2,
    double Y2,
    double Confidence,
    string? Subtype,
    double? SubtypeConfidence);
