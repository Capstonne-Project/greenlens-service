using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// Tier 2 duplicate detection: compares the new report's image with its Tier 1 candidate
/// using the AI image-compare service (DINOv2). Runs out of band so submit stays fast.
/// </summary>
/// <remarks>
/// Implements: BR-REP-030, BR-REP-031, BR-AI-002 (image similarity), BR-AI-006 (timeout → keep Tier 1).
/// Idempotent: only acts while the report is still a Tier 1 (geo_category) possible duplicate.
/// Decision matrix:
///   AI same scene   → upgrade source to geo_category_ai + record confidence
///   AI different    → dismiss the possible-duplicate flag
///   AI null/timeout → keep Tier 1 (geo_category)
/// </remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class CompareDuplicateImagesJob(
    ApplicationDbContext db,
    IAiImageCompareService aiImageCompare,
    ILogger<CompareDuplicateImagesJob> logger)
{
    public async Task ExecuteAsync(Guid reportId, Guid candidateReportId, CancellationToken ct = default)
    {
        if (!await StillNeedsTier2CompareAsync(reportId, ct).ConfigureAwait(false))
        {
            logger.LogDebug("CompareDuplicateImagesJob: report {Id} no longer needs Tier 2, skipping", reportId);
            return;
        }

        if (!await db.Reports.AsNoTracking().AnyAsync(r => r.Id == candidateReportId, ct).ConfigureAwait(false))
        {
            logger.LogDebug("CompareDuplicateImagesJob: candidate {Id} not found, keeping Tier 1", candidateReportId);
            return;
        }

        var reportImage = await GetFirstImageUrlAsync(reportId, ct).ConfigureAwait(false);
        var candidateImage = await GetFirstImageUrlAsync(candidateReportId, ct).ConfigureAwait(false);
        if (reportImage is null || candidateImage is null)
        {
            logger.LogDebug("CompareDuplicateImagesJob: missing image URL, keeping Tier 1 for {Id}", reportId);
            return;
        }

        var result = await aiImageCompare.CompareAsync(reportImage, candidateImage, ct).ConfigureAwait(false);
        if (result is null)
        {
            // AI unavailable / timeout → keep Tier 1 (geo_category).
            logger.LogInformation("CompareDuplicateImagesJob: AI unavailable for {Id}, keeping Tier 1", reportId);
            return;
        }

        // Re-load after the async AI call so concurrent confirm/dismiss is not overwritten.
        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId, ct)
            .ConfigureAwait(false);

        if (report is null || !StillNeedsTier2Compare(report))
        {
            logger.LogInformation(
                "CompareDuplicateImagesJob: report {Id} state changed during AI compare, skipping apply",
                reportId);
            return;
        }

        report.ApplyDuplicateAiResult(result.IsSameScene, result.Confidence);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "CompareDuplicateImagesJob: report {Id} — confidence {Confidence}, sameScene {SameScene}, model {Model}",
            reportId, result.Confidence, result.IsSameScene, result.Model);
    }

    private static bool StillNeedsTier2Compare(Domain.Entities.Report? report) =>
        report is not null
        && report.IsPossibleDuplicate
        && DuplicateDetectionSources.IsTier1PendingAi(report.DuplicateDetectionSource)
        && report.Status is not (ReportStatus.Duplicate or ReportStatus.Rejected);

    private async Task<bool> StillNeedsTier2CompareAsync(Guid reportId, CancellationToken ct)
    {
        var state = await db.Reports.AsNoTracking()
            .Where(r => r.Id == reportId)
            .Select(r => new { r.IsPossibleDuplicate, r.DuplicateDetectionSource, r.Status })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return state is not null
               && state.IsPossibleDuplicate
               && DuplicateDetectionSources.IsTier1PendingAi(state.DuplicateDetectionSource)
               && state.Status is not (ReportStatus.Duplicate or ReportStatus.Rejected);
    }

    private async Task<string?> GetFirstImageUrlAsync(Guid reportId, CancellationToken ct)
        => await db.ReportMedia.AsNoTracking()
            .Where(m => m.ReportId == reportId && m.Type == MediaType.Image)
            .Select(m => m.Url)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
}
