using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Mappings;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// BR-AI-006: Retry AI classification for reports still tagged ai_pending within 1 hour of creation.
/// Runs every 5 minutes.
/// </summary>
[AutomaticRetry(Attempts = 2)]
internal sealed class AiRetryJob(
    ApplicationDbContext db,
    IAiClassificationService aiService,
    IHttpClientFactory httpClientFactory,
    ILogger<AiRetryJob> logger)
{
    private const int BatchSize = 50;
    private static readonly TimeSpan RetryWindow = TimeSpan.FromHours(1);

    public async Task ExecuteAsync()
    {
        logger.LogInformation("AiRetryJob: Starting...");

        var cutoff = DateTime.UtcNow.Subtract(RetryWindow);

        var pending = await db.Reports
            .Where(r => r.AiPending
                        && r.Status == ReportStatus.Submitted
                        && r.CreatedAt >= cutoff)
            .OrderBy(r => r.CreatedAt)
            .Take(BatchSize)
            .Include(r => r.Media)
            .ToListAsync()
            .ConfigureAwait(false);

        if (pending.Count == 0)
        {
            logger.LogInformation("AiRetryJob: No pending reports in retry window.");
            return;
        }

        var client = httpClientFactory.CreateClient();
        var succeeded = 0;

        foreach (var report in pending)
        {
            var image = report.Media.FirstOrDefault(m => m.Type == MediaType.Image);
            if (image is null)
            {
                logger.LogWarning("AiRetryJob: Report {ReportId} has no image — skip", report.Id);
                continue;
            }

            try
            {
                await using var stream = await client.GetStreamAsync(image.Url).ConfigureAwait(false);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;

                var result = await aiService.ClassifyAsync(
                    ms, Path.GetFileName(image.Url), image.MimeType).ConfigureAwait(false);

                if (result is null)
                {
                    logger.LogDebug("AiRetryJob: AI still unavailable for report {ReportId}", report.Id);
                    continue;
                }

                var severity = AiSeverityMapper.Parse(result.Classify.Severity);
                var classifiedType = result.Classify.PrimaryClass ?? "unknown";
                report.ApplyAiResults(classifiedType, (decimal)result.Classify.Confidence, severity);
                succeeded++;

                logger.LogInformation(
                    "AiRetryJob: Classified report {ReportId} as {Type} ({Confidence:P0})",
                    report.Id, classifiedType, result.Classify.Confidence);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "AiRetryJob: Failed to classify report {ReportId}", report.Id);
            }
        }

        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation(
            "AiRetryJob: Completed. Processed {Total}, cleared ai_pending on {Succeeded}",
            pending.Count, succeeded);
    }
}
