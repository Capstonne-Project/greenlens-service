using Greenlens.Application.Features.Catalog.GetPollutionCategories;

namespace Greenlens.Application.Features.Reports.AnalyzeReportImage;

public sealed record AnalyzeReportImageResponse(
    string TempImageId,
    int ExpiresInSeconds,
    AiResultDto AiResult,
    /// <summary>Catalog row from AI primary_class — FE uses <c>id</c> + <c>nameVi</c> to auto-fill the form.</summary>
    PollutionCategoryListItemDto? SuggestedCategory);

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
    int BboxCount);
