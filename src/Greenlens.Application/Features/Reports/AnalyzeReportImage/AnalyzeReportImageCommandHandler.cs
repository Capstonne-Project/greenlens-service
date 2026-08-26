using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Mappings;
using Greenlens.Application.Features.Catalog.GetPollutionCategories;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.AnalyzeReportImage;

/// <summary>
/// Step 1: validate image → call AI Service → save to temp store → return temp_image_id + ai_result.
/// </summary>
/// <remarks>
/// Implements: BR-AI-001, BR-AI-006 (timeout → service unavailable 503),
/// BR-REP-001 (file must be image), BR-REP-002 (size ≤ 20MB checked in validator),
/// BR-REP-005 (suggested category id + labels for FE auto-fill).
/// </remarks>
public sealed class AnalyzeReportImageCommandHandler(
    IAiClassificationService aiService,
    ITempImageStore tempStore,
    IPollutionCategoryRepository categories,
    IReportDescriptionGenerator descriptionGenerator,
    ILogger<AnalyzeReportImageCommandHandler> logger)
    : IRequestHandler<AnalyzeReportImageCommand, Result<AnalyzeReportImageResponse>>
{
    private const int TempTtlSeconds = 900; // 15 minutes

    public async Task<Result<AnalyzeReportImageResponse>> Handle(
        AnalyzeReportImageCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Analyzing report image for file {FileName}", request.FileName);

        if (!ReportImageContentTypes.TryResolve(request.FileName, request.ContentType, out var contentType))
        {
            logger.LogWarning("Invalid image type for file {FileName}", request.FileName);
            return Errors.Media.InvalidImageType;
        }

        // Call AI Service — timeout/down returns null (BR-AI-006)
        using var stream = new MemoryStream(request.ImageBytes);
        var aiResult = await aiService.ClassifyAsync(
            stream, request.FileName, contentType, cancellationToken)
            .ConfigureAwait(false);

        if (aiResult is null)
        {
            logger.LogWarning("AI service unavailable for image {FileName}", request.FileName);
            return Errors.Ai.ServiceUnavailable;
        }

        // Save temp regardless of AI decision (FE needs to show the result to user)
        var tempId = await tempStore.SaveAsync(
            request.ImageBytes,
            request.FileName,
            contentType,
            aiResult,
            ct: cancellationToken)
            .ConfigureAwait(false);

        var suggestedCategory = await ResolveSuggestedCategoryAsync(
                aiResult.Classify.PrimaryClass,
                cancellationToken)
            .ConfigureAwait(false);

        var suggestedDescription = await GenerateSuggestedDescriptionAsync(
                aiResult,
                suggestedCategory,
                cancellationToken)
            .ConfigureAwait(false);

        var response = new AnalyzeReportImageResponse(
            TempImageId: tempId,
            ExpiresInSeconds: TempTtlSeconds,
            AiResult: MapAiResult(aiResult),
            SuggestedCategory: suggestedCategory,
            SuggestedDescription: suggestedDescription);

        logger.LogInformation("Image analyzed: {FileName}, decision={Decision}, category={Category}",
            request.FileName, aiResult.Decision, aiResult.Classify.PrimaryClass);

        return response;
    }

    private async Task<PollutionCategoryListItemDto?> ResolveSuggestedCategoryAsync(
        string? primaryClass,
        CancellationToken cancellationToken)
    {
        var categoryCode = AiPollutionClassMapper.ToCategoryCode(primaryClass);
        if (categoryCode is null)
            return null;

        var category = await categories.GetActiveByCodeAsync(categoryCode, cancellationToken)
            .ConfigureAwait(false);

        return category is null
            ? null
            : new PollutionCategoryListItemDto(
                category.Id,
                category.Code,
                category.NameVi,
                category.NameEn,
                category.IconUrl);
    }

    private async Task<string?> GenerateSuggestedDescriptionAsync(
        Application.Common.Interfaces.AiClassificationResult aiResult,
        PollutionCategoryListItemDto? suggestedCategory,
        CancellationToken cancellationToken)
    {
        if (suggestedCategory is null ||
            aiResult.Decision == Application.Common.Interfaces.AiDecision.IrrelevantOrSuspectedAbusive)
        {
            return null;
        }

        var subtypes = aiResult.Classify.Predictions
            .SelectMany(p => p.Subtypes ?? [])
            .GroupBy(s => s.Subtype)
            .Select(g => new ReportDescriptionSubtype(g.Key, g.Sum(s => s.Count)))
            .ToArray();
        var subtypeLabels = subtypes.Select(s => s.Label).ToArray();

        return await descriptionGenerator.GenerateAsync(
                new ReportDescriptionContext(
                    suggestedCategory.NameVi,
                    aiResult.Classify.Severity,
                    subtypeLabels,
                    aiResult.Classify.PollutionCoverageRatio,
                    subtypes),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static AiResultDto MapAiResult(Application.Common.Interfaces.AiClassificationResult result)
    {
        var decisionStr = result.Decision switch
        {
            Application.Common.Interfaces.AiDecision.AcceptableReportImage => "ACCEPTABLE_REPORT_IMAGE",
            Application.Common.Interfaces.AiDecision.NeedManualReview => "NEED_MANUAL_REVIEW",
            _ => "IRRELEVANT_OR_SUSPECTED_ABUSIVE"
        };

        var predictions = result.Classify.Predictions
            .Select(p => new AiPredictionDto(
                p.Class,
                p.Confidence,
                p.BboxCount,
                p.Subtypes?
                    .Select(s => new AiTrashSubtypeDto(s.Subtype, s.Count, s.Confidence))
                    .ToArray(),
                p.Boxes?
                    .Select(b => new AiBoxDto(b.X1, b.Y1, b.X2, b.Y2, b.Confidence, b.Subtype, b.SubtypeConfidence))
                    .ToArray()))
            .ToArray();

        var classify = new AiClassifyDto(
            result.Classify.PrimaryClass,
            result.Classify.Confidence,
            result.Classify.Severity,
            result.Classify.ImageRelevance,
            result.Classify.PollutionCoverageRatio,
            predictions,
            result.Classify.InferenceTimeMs,
            result.Classify.YoloActive,
            result.Classify.SceneClassifierActive);

        return new AiResultDto(decisionStr, result.Reason, classify);
    }
}
