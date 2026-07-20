using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Mappings;
using Greenlens.Application.Features.Catalog.GetPollutionCategories;
using Greenlens.Application.Features.Reports.AnalyzeReportImage;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.AnalyzeUploadedReportImage;

/// <summary>
/// Download an owned R2 object and run synchronous AI pre-analysis.
/// </summary>
/// <remarks>
/// Implements: BR-AI-001 (classification), BR-AI-006 (5-second timeout fallback),
/// BR-REP-001 (image required), BR-REP-002 (image size limit).
/// The image is uploaded once by Mobile and reused during report submission.
/// </remarks>
public sealed class AnalyzeUploadedReportImageCommandHandler(
    IFileStorageService fileStorage,
    IAiClassificationService aiService,
    ITempImageStore tempStore,
    IPollutionCategoryRepository categories,
    ILogger<AnalyzeUploadedReportImageCommandHandler> logger)
    : IRequestHandler<AnalyzeUploadedReportImageCommand, Result<AnalyzeReportImageResponse>>
{
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;
    private const int TempTtlSeconds = 900;

    public async Task<Result<AnalyzeReportImageResponse>> Handle(
        AnalyzeUploadedReportImageCommand request,
        CancellationToken cancellationToken)
    {
        if (!fileStorage.IsOwnedPublicUrl(request.PublicUrl, request.Key))
            return Errors.Media.InvalidStorageUrl;

        if (!ReportImageContentTypes.TryResolve(
                request.FileName,
                request.ContentType,
                out var contentType))
            return Errors.Media.InvalidImageType;

        var stored = await fileStorage.DownloadAsync(
                request.Key,
                MaxImageSizeBytes,
                cancellationToken)
            .ConfigureAwait(false);

        if (stored is null)
            return Errors.Media.UploadNotFound;

        if (stored.SizeBytes != request.SizeBytes)
            return Errors.Media.UploadMetadataMismatch;

        using var stream = new MemoryStream(stored.Bytes, writable: false);
        var aiResult = await aiService.ClassifyAsync(
                stream,
                request.FileName,
                contentType,
                cancellationToken)
            .ConfigureAwait(false);

        if (aiResult is null)
            return Errors.Ai.ServiceUnavailable;

        var analysisId = await tempStore.SaveAsync(
                stored.Bytes,
                request.FileName,
                contentType,
                aiResult,
                request.PublicUrl.Trim(),
                request.Key.Trim(),
                cancellationToken)
            .ConfigureAwait(false);

        var suggestedCategory = await ResolveSuggestedCategoryAsync(
                aiResult.Classify.PrimaryClass,
                cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Uploaded R2 image analyzed. Key={Key}, decision={Decision}, category={Category}",
            request.Key,
            aiResult.Decision,
            aiResult.Classify.PrimaryClass);

        return new AnalyzeReportImageResponse(
            analysisId,
            TempTtlSeconds,
            MapAiResult(aiResult),
            suggestedCategory);
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

    private static AiResultDto MapAiResult(AiClassificationResult result)
    {
        var decision = result.Decision switch
        {
            AiDecision.AcceptableReportImage => "ACCEPTABLE_REPORT_IMAGE",
            AiDecision.NeedManualReview => "NEED_MANUAL_REVIEW",
            _ => "IRRELEVANT_OR_SUSPECTED_ABUSIVE"
        };

        var classify = result.Classify;
        return new AiResultDto(
            decision,
            result.Reason,
            new AiClassifyDto(
                classify.PrimaryClass,
                classify.Confidence,
                classify.Severity,
                classify.ImageRelevance,
                classify.PollutionCoverageRatio,
                classify.Predictions
                    .Select(p => new AiPredictionDto(p.Class, p.Confidence, p.BboxCount))
                    .ToArray(),
                classify.InferenceTimeMs,
                classify.YoloActive,
                classify.SceneClassifierActive));
    }
}
