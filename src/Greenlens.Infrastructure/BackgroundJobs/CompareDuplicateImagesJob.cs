using Greenlens.Application.Common.Interfaces;
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
/// Idempotent: only acts while the report is still a "geo_time" possible duplicate.
/// Decision matrix:
///   AI same scene   → upgrade source to "geo_time_ai" + record similarity
///   AI different    → dismiss the possible-duplicate flag
///   AI null/timeout → keep Tier 1 (geo_time)
/// </remarks>
[AutomaticRetry(Attempts = 2)]
internal sealed class CompareDuplicateImagesJob(
    ApplicationDbContext db,
    IAiImageCompareService aiImageCompare,
    ILogger<CompareDuplicateImagesJob> logger)
{
    public async Task ExecuteAsync(Guid reportId, Guid candidateReportId, CancellationToken ct = default)
    {
        var report = await db.Reports
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == reportId, ct)
            .ConfigureAwait(false);

        // Idempotency: skip if already resolved/dismissed or already upgraded by a prior run.
        if (report is null
            || !report.IsPossibleDuplicate
            || report.DuplicateDetectionSource != "geo_time")
        {
            logger.LogDebug("CompareDuplicateImagesJob: report {Id} no longer needs Tier 2, skipping", reportId);
            return;
        }

        var candidate = await db.Reports
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == candidateReportId, ct)
            .ConfigureAwait(false);
        if (candidate is null)
        {
            logger.LogDebug("CompareDuplicateImagesJob: candidate {Id} not found, keeping Tier 1", candidateReportId);
            return;
        }

        var reportImage = FirstImageUrl(report.Media);
        var candidateImage = FirstImageUrl(candidate.Media);
        if (reportImage is null || candidateImage is null)
        {
            logger.LogDebug("CompareDuplicateImagesJob: missing image URL, keeping Tier 1 for {Id}", reportId);
            return;
        }

        var result = await aiImageCompare.CompareAsync(reportImage, candidateImage, ct).ConfigureAwait(false);
        if (result is null)
        {
            // AI unavailable / timeout → keep Tier 1 (geo_time).
            logger.LogInformation("CompareDuplicateImagesJob: AI unavailable for {Id}, keeping Tier 1", reportId);
            return;
        }

        report.ApplyDuplicateAiResult(result.IsSameScene, result.Similarity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "CompareDuplicateImagesJob: report {Id} — similarity {Similarity}, sameScene {SameScene}, model {Model}",
            reportId, result.Similarity, result.IsSameScene, result.Model);
    }

    private static string? FirstImageUrl(IEnumerable<Domain.Entities.ReportMedia> media)
        => media.Where(m => m.Type == MediaType.Image).Select(m => m.Url).FirstOrDefault();
}
