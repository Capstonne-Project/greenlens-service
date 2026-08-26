using System.Diagnostics;
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
/// Implements: BR-AI-001 (classification), BR-AI-006 (timeout fallback),
/// BR-REP-001 (image required), BR-REP-002 (image size limit).
/// The image is uploaded once by Mobile and reused during report submission.
/// </remarks>
public sealed class AnalyzeUploadedReportImageCommandHandler(
    IFileStorageService fileStorage,
    IAiClassificationService aiService,
    ITempImageStore tempStore,
    IPollutionCategoryRepository categories,
    IReportDescriptionGenerator descriptionGenerator,
    ILogger<AnalyzeUploadedReportImageCommandHandler> logger)
    : IRequestHandler<AnalyzeUploadedReportImageCommand, Result<AnalyzeReportImageResponse>>
{
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;
    private const int TempTtlSeconds = 900;

    public async Task<Result<AnalyzeReportImageResponse>> Handle(
        AnalyzeUploadedReportImageCommand request,
        CancellationToken cancellationToken)
    {
        var totalSw = Stopwatch.StartNew();

        logger.LogInformation(
            "[AI-DIAG] analyze-uploaded HIT | key={Key} file={FileName} sizeBytes={SizeBytes} contentType={ContentType} urlHost={UrlHost}",
            request.Key,
            request.FileName,
            request.SizeBytes,
            request.ContentType,
            TryGetHost(request.PublicUrl));

        if (!fileStorage.IsOwnedPublicUrl(request.PublicUrl, request.Key))
        {
            logger.LogWarning(
                "[AI-DIAG] analyze-uploaded REJECT InvalidStorageUrl | key={Key} url={Url}",
                request.Key,
                request.PublicUrl);
            return Errors.Media.InvalidStorageUrl;
        }

        if (!ReportImageContentTypes.TryResolve(
                request.FileName,
                request.ContentType,
                out var contentType))
        {
            logger.LogWarning(
                "[AI-DIAG] analyze-uploaded REJECT InvalidImageType | file={FileName} contentType={ContentType}",
                request.FileName,
                request.ContentType);
            return Errors.Media.InvalidImageType;
        }

        var downloadSw = Stopwatch.StartNew();
        var stored = await fileStorage.DownloadAsync(
                request.Key,
                MaxImageSizeBytes,
                cancellationToken)
            .ConfigureAwait(false);
        downloadSw.Stop();

        if (stored is null)
        {
            logger.LogWarning(
                "[AI-DIAG] analyze-uploaded REJECT UploadNotFound after R2 download {DownloadMs}ms | key={Key}",
                downloadSw.ElapsedMilliseconds,
                request.Key);
            return Errors.Media.UploadNotFound;
        }

        logger.LogInformation(
            "[AI-DIAG] R2 download OK in {DownloadMs}ms | key={Key} bytes={Bytes}",
            downloadSw.ElapsedMilliseconds,
            request.Key,
            stored.SizeBytes);

        if (stored.SizeBytes != request.SizeBytes)
        {
            logger.LogWarning(
                "[AI-DIAG] analyze-uploaded REJECT UploadMetadataMismatch | key={Key} claimed={Claimed} actual={Actual}",
                request.Key,
                request.SizeBytes,
                stored.SizeBytes);
            return Errors.Media.UploadMetadataMismatch;
        }

        using var stream = new MemoryStream(stored.Bytes, writable: false);
        var aiResult = await aiService.ClassifyAsync(
                stream,
                request.FileName,
                contentType,
                cancellationToken)
            .ConfigureAwait(false);

        if (aiResult is null)
        {
            totalSw.Stop();
            logger.LogWarning(
                "[AI-DIAG] analyze-uploaded FAIL AI_SERVICE_UNAVAILABLE after {TotalMs}ms | key={Key} file={FileName} — xem log [AI-DIAG] Classify * phía trên",
                totalSw.ElapsedMilliseconds,
                request.Key,
                request.FileName);
            return Errors.Ai.ServiceUnavailable;
        }

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

        var suggestedDescription = await GenerateSuggestedDescriptionAsync(
                aiResult,
                suggestedCategory,
                cancellationToken)
            .ConfigureAwait(false);

        totalSw.Stop();
        logger.LogInformation(
            "[AI-DIAG] analyze-uploaded OK in {TotalMs}ms | key={Key} decision={Decision} primary={Primary} conf={Confidence:F3} suggestedCategory={CategoryCode} tempImageId={TempId}",
            totalSw.ElapsedMilliseconds,
            request.Key,
            aiResult.Decision,
            aiResult.Classify.PrimaryClass,
            aiResult.Classify.Confidence,
            suggestedCategory?.Code ?? "(none)",
            analysisId);

        return new AnalyzeReportImageResponse(
            analysisId,
            TempTtlSeconds,
            MapAiResult(aiResult),
            suggestedCategory,
            suggestedDescription);
    }

    private static string TryGetHost(string url)
    {
        try
        {
            return new Uri(url).Host;
        }
        catch
        {
            return "(invalid-url)";
        }
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
        AiClassificationResult aiResult,
        PollutionCategoryListItemDto? suggestedCategory,
        CancellationToken cancellationToken)
    {
        if (suggestedCategory is null || aiResult.Decision == AiDecision.IrrelevantOrSuspectedAbusive)
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
                    .ToArray(),
                classify.InferenceTimeMs,
                classify.YoloActive,
                classify.SceneClassifierActive));
    }
}
