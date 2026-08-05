using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Reports.Common;

internal static class CitizenReportMediaLoader
{
    public static async Task<IReadOnlyDictionary<Guid, List<ReportReviewMediaItem>>> LoadByReportIdsAsync(
        IReportMediaRepository reportMedia,
        IReadOnlyCollection<Guid> reportIds,
        CancellationToken ct)
    {
        if (reportIds.Count == 0)
            return new Dictionary<Guid, List<ReportReviewMediaItem>>();

        var rows = await reportMedia.QueryAsNoTracking()
            .Where(m => reportIds.Contains(m.ReportId))
            .Where(m => m.Type == MediaType.Image || m.Type == MediaType.Video)
            .Where(m => m.ReopenRequestId == null)
            .OrderBy(m => m.UploadedAt)
            .Select(m => new
            {
                m.ReportId,
                m.Id,
                m.Url,
                m.ThumbnailUrl,
                m.Type,
                m.UploadedAt
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .GroupBy(m => m.ReportId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => new ReportReviewMediaItem(
                    m.Id, m.Url, m.ThumbnailUrl, m.Type, m.UploadedAt)).ToList());
    }

    public static IReadOnlyList<ReportReviewMediaItem> GetMediaOrEmpty(
        IReadOnlyDictionary<Guid, List<ReportReviewMediaItem>> byReportId,
        Guid reportId) =>
        byReportId.TryGetValue(reportId, out var media) ? media : [];

    /// <summary>First citizen image/video per report (list/card thumbnail).</summary>
    public static async Task<IReadOnlyDictionary<Guid, ReportReviewMediaItem>> LoadFirstByReportIdsAsync(
        IReportMediaRepository reportMedia,
        IReadOnlyCollection<Guid> reportIds,
        CancellationToken ct)
    {
        if (reportIds.Count == 0)
            return new Dictionary<Guid, ReportReviewMediaItem>();

        var rows = await reportMedia.QueryAsNoTracking()
            .Where(m => reportIds.Contains(m.ReportId))
            .Where(m => m.Type == MediaType.Image || m.Type == MediaType.Video)
            .Where(m => m.ReopenRequestId == null)
            .OrderBy(m => m.UploadedAt)
            .Select(m => new
            {
                m.ReportId,
                m.Id,
                m.Url,
                m.ThumbnailUrl,
                m.Type,
                m.UploadedAt
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .GroupBy(m => m.ReportId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var first = g.First();
                    return new ReportReviewMediaItem(
                        first.Id,
                        first.Url,
                        first.ThumbnailUrl,
                        first.Type,
                        first.UploadedAt);
                });
    }

    public static IReadOnlyList<ReportReviewMediaItem> GetFirstMediaList(
        IReadOnlyDictionary<Guid, ReportReviewMediaItem> byReportId,
        Guid reportId) =>
        byReportId.TryGetValue(reportId, out var media) ? [media] : [];
}
