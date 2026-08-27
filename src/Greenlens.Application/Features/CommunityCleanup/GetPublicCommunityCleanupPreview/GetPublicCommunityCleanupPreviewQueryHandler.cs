using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Options;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Application.Features.CommunityCleanup.GetPublicCommunityCleanupPreview;

/// <summary>
/// Anonymous preview for Next.js OG pages and social crawlers (Facebook, X, LinkedIn).
/// </summary>
/// <remarks>
/// Implements: BR-AUTH-014 (anonymous public read), BR-CMU-015 (no participant PII).
/// Cancelled events return NotFound so revoked share links do not expose program details.
/// </remarks>
public sealed class GetPublicCommunityCleanupPreviewQueryHandler(
    ICommunityCleanupEventRepository events,
    ICommunityCleanupParticipantRepository participants,
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IOptions<PublicWebOptions> publicWebOptions,
    ILogger<GetPublicCommunityCleanupPreviewQueryHandler> logger)
    : IRequestHandler<GetPublicCommunityCleanupPreviewQuery, Result<CommunityCleanupPublicPreviewResponse>>
{
    public async Task<Result<CommunityCleanupPublicPreviewResponse>> Handle(
        GetPublicCommunityCleanupPreviewQuery request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);

        if (ev is null || ev.Status == CommunityCleanupStatus.Cancelled)
        {
            logger.LogWarning("Public community cleanup preview not found for ID {EventId}", request.EventId);
            return Errors.CommunityCleanup.EventNotFound;
        }

        var report = await reports.Query()
            .Include(r => r.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == ev.ReportId, ct)
            .ConfigureAwait(false);

        if (report is null)
            return Errors.Reports.ReportNotFound;

        var participantCount = await participants.CountActiveByEventIdAsync(ev.Id, ct).ConfigureAwait(false);

        var thumbnailUrl = await reportMedia.QueryAsNoTracking()
            .Where(m => m.ReportId == ev.ReportId && m.Type == MediaType.Image)
            .OrderBy(m => m.UploadedAt)
            .Select(m => m.ThumbnailUrl ?? m.Url)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var share = CommunityCleanupShareBuilder.Build(ev, report, thumbnailUrl, publicWebOptions.Value);

        return new CommunityCleanupPublicPreviewResponse(
            ev.Id,
            ev.Title,
            ev.Description,
            ev.Status,
            ev.StartsAt,
            ev.EndsAt,
            ev.JoinClosesAt,
            ev.MaxParticipants,
            participantCount,
            Math.Max(0, ev.MaxParticipants - participantCount),
            ev.MeetingNote,
            report.Category.NameVi,
            report.Address,
            thumbnailUrl,
            share);
    }
}
