using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Options;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Application.Features.CommunityCleanup.Common;

/// <summary>
/// Builds share payload and publishes a link post to the configured Facebook Page.
/// </summary>
internal static class CommunityCleanupFacebookPageShare
{
    public static async Task<Result<CommunityCleanupFacebookAutoPostDto>> PublishAsync(
        CommunityCleanupEvent ev,
        IReportRepository reports,
        IReportMediaRepository reportMedia,
        IOptions<PublicWebOptions> publicWebOptions,
        IOptions<MetaPageOptions> metaPageOptions,
        IFacebookPagePublisher facebookPagePublisher,
        ILogger logger,
        CancellationToken ct)
    {
        var meta = metaPageOptions.Value;
        if (!meta.AutoPostEnabled)
            return Errors.Meta.FeatureDisabled;

        if (!meta.IsConfigured)
            return Errors.Meta.NotConfigured;

        var report = await reports.Query()
            .Include(r => r.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == ev.ReportId, ct)
            .ConfigureAwait(false);

        if (report is null)
            return Errors.Reports.ReportNotFound;

        var thumbnailUrl = await reportMedia.QueryAsNoTracking()
            .Where(m => m.ReportId == ev.ReportId && m.Type == Domain.Enums.MediaType.Image)
            .OrderBy(m => m.UploadedAt)
            .Select(m => m.ThumbnailUrl ?? m.Url)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var share = CommunityCleanupShareBuilder.Build(ev, report, thumbnailUrl, publicWebOptions.Value);

        if (string.IsNullOrWhiteSpace(share.ImageUrl))
            return Errors.Meta.ShareImageRequired;

        var postResult = await facebookPagePublisher.PublishPhotoPostAsync(
            share.Caption,
            share.ImageUrl,
            ct).ConfigureAwait(false);

        if (postResult.IsSuccess)
        {
            return new CommunityCleanupFacebookAutoPostDto(
                Attempted: true,
                Success: true,
                PostId: postResult.Value,
                PageUrl: BuildFacebookPostUrl(postResult.Value!),
                ErrorCode: null,
                ErrorMessage: null);
        }

        logger.LogWarning(
            "Facebook Page share failed for community cleanup {EventId}: {ErrorCode}",
            ev.Id,
            postResult.Error?.Code);

        return new CommunityCleanupFacebookAutoPostDto(
            Attempted: true,
            Success: false,
            PostId: null,
            PageUrl: null,
            ErrorCode: postResult.Error?.Code,
            ErrorMessage: postResult.Error?.Message);
    }

    private static string BuildFacebookPostUrl(string postId) =>
        FacebookPostUrl.FromPostId(postId);
}
