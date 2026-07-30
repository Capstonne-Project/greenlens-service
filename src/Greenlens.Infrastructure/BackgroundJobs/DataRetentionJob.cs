using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// Enforces data retention policy:
///   - ReportMedia files (photos/videos): delete S3 files older than 2 years, keep DB record.
///   - AuditLog (BR-ADM-010): hard-delete entries older than 12 months.
///   - ReportStatusHistory: hard-delete records older than 12 months.
/// Runs weekly via Hangfire.
/// </summary>
/// <remarks>Implements: BR-DAT-002 (retention: photos 2y, audit log 12m).</remarks>
internal sealed class DataRetentionJob(
    ApplicationDbContext dbContext,
    IFileStorageService fileStorage,
    ILogger<DataRetentionJob> logger)
{
    private const int MediaRetentionYears = 2;
    private const int AuditLogRetentionMonths = 12;
    private const int MediaBatchSize = 100;
    private const int AuditLogBatchSize = 1000;
    private const int HistoryBatchSize = 1000;
    private const string DeletedPlaceholder = "[deleted-by-retention-policy]";

    public async Task ExecuteAsync()
    {
        await CleanupExpiredMediaAsync().ConfigureAwait(false);
        await CleanupExpiredAuditLogsAsync().ConfigureAwait(false);
        await CleanupExpiredReportStatusHistoryAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Phase 1: Delete S3 files for ReportMedia older than 2 years.
    /// Keeps the DB record (metadata) but replaces Url with placeholder.
    /// </summary>
    private async Task CleanupExpiredMediaAsync()
    {
        var threshold = DateTime.UtcNow.AddYears(-MediaRetentionYears);

        var expiredMedia = await dbContext.ReportMedia
            .Where(m => m.UploadedAt <= threshold && m.Url != DeletedPlaceholder)
            .OrderBy(m => m.UploadedAt)
            .Take(MediaBatchSize)
            .ToListAsync()
            .ConfigureAwait(false);

        if (expiredMedia.Count == 0)
        {
            logger.LogInformation("DataRetentionJob: no expired media to clean up");
            return;
        }

        var deletedCount = 0;
        foreach (var media in expiredMedia)
        {
            try
            {
                // Extract S3 key from URL (the key is the path portion)
                var key = ExtractKeyFromUrl(media.Url);
                if (!string.IsNullOrEmpty(key))
                {
                    await fileStorage.DeleteAsync(key).ConfigureAwait(false);
                }

                // Also delete thumbnail if exists
                if (!string.IsNullOrEmpty(media.ThumbnailUrl))
                {
                    var thumbKey = ExtractKeyFromUrl(media.ThumbnailUrl);
                    if (!string.IsNullOrEmpty(thumbKey))
                    {
                        await fileStorage.DeleteAsync(thumbKey).ConfigureAwait(false);
                    }
                }

                // Mark record as deleted — keep metadata for audit trail
                dbContext.Entry(media).Property("Url").CurrentValue = DeletedPlaceholder;
                if (media.ThumbnailUrl is not null)
                    dbContext.Entry(media).Property("ThumbnailUrl").CurrentValue = DeletedPlaceholder;

                deletedCount++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "DataRetentionJob: failed to delete media {MediaId}, will retry next run",
                    media.Id);
            }
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation(
            "DataRetentionJob: cleaned up {Count}/{Total} expired media files (threshold={Threshold:yyyy-MM-dd})",
            deletedCount, expiredMedia.Count, threshold);
    }

    /// <summary>
    /// Phase 2: Hard-delete AuditLog entries older than 12 months (BR-ADM-010).
    /// </summary>
    private async Task CleanupExpiredAuditLogsAsync()
    {
        var threshold = DateTime.UtcNow.AddMonths(-AuditLogRetentionMonths);
        var totalDeleted = 0;

        while (true)
        {
            var deletedCount = await dbContext.AuditLogs
                .Where(a => a.CreatedAt <= threshold)
                .Take(AuditLogBatchSize)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);

            totalDeleted += deletedCount;
            if (deletedCount < AuditLogBatchSize)
                break;
        }

        logger.LogInformation(
            "DataRetentionJob: deleted {Count} expired audit_logs entries (threshold={Threshold:yyyy-MM-dd})",
            totalDeleted, threshold);
    }

    /// <summary>
    /// Phase 3: Hard-delete ReportStatusHistory records older than 12 months.
    /// </summary>
    private async Task CleanupExpiredReportStatusHistoryAsync()
    {
        var threshold = DateTime.UtcNow.AddMonths(-AuditLogRetentionMonths);

        var deletedCount = await dbContext.ReportStatusHistory
            .Where(h => h.CreatedAt <= threshold)
            .Take(HistoryBatchSize)
            .ExecuteDeleteAsync()
            .ConfigureAwait(false);

        logger.LogInformation(
            "DataRetentionJob: deleted {Count} expired report status history entries (threshold={Threshold:yyyy-MM-dd})",
            deletedCount, threshold);
    }

    private static string? ExtractKeyFromUrl(string url)
    {
        // URL format: https://bucket.s3.region.amazonaws.com/key
        // or https://cdn.example.com/key
        if (string.IsNullOrEmpty(url) || url == DeletedPlaceholder)
            return null;

        try
        {
            var uri = new Uri(url);
            // Return path without leading slash
            return uri.AbsolutePath.TrimStart('/');
        }
        catch
        {
            return null;
        }
    }
}
