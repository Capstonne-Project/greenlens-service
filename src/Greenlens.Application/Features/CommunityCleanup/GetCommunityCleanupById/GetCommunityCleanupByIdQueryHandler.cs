using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.GetCommunityCleanupById;

/// <summary>Full detail — docs/community-cleanup-feature-spec.md §7.4. No participant PII (draft BR-CMU-015).</summary>
public sealed class GetCommunityCleanupByIdQueryHandler(
    ICommunityCleanupEventRepository events,
    ICommunityCleanupParticipantRepository participants,
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IUserRepository users,
    IEnvironmentalTeamRepository teams,
    ICurrentUser currentUser,
    ILogger<GetCommunityCleanupByIdQueryHandler> logger)
    : IRequestHandler<GetCommunityCleanupByIdQuery, Result<CommunityCleanupEventDetailResponse>>
{
    public async Task<Result<CommunityCleanupEventDetailResponse>> Handle(
        GetCommunityCleanupByIdQuery request, CancellationToken ct)
    {
        var ev = await events.QueryAsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
        {
            logger.LogWarning("Community cleanup event not found for ID {EventId}", request.EventId);
            return Errors.CommunityCleanup.EventNotFound;
        }

        var report = await reports.Query()
            .Include(r => r.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == ev.ReportId, ct)
            .ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        var leaderUser = await users.GetByIdAsync(ev.LeaderUserId, ct).ConfigureAwait(false);
        var leaderTeam = await teams.GetByIdAsync(ev.LeaderTeamId, ct).ConfigureAwait(false);
        if (leaderUser is null || leaderTeam is null)
            return Errors.CommunityCleanup.EventNotFound;

        var participantCount = await participants.CountActiveByEventIdAsync(ev.Id, ct).ConfigureAwait(false);

        var mediaCounts = await reportMedia.QueryAsNoTracking()
            .Where(m => m.ReportId == ev.ReportId
                && (m.Type == MediaType.Before || m.Type == MediaType.Progress || m.Type == MediaType.After))
            .GroupBy(m => m.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var mediaSummary = new CommunityCleanupMediaSummaryDto(
            mediaCounts.FirstOrDefault(x => x.Type == MediaType.Before)?.Count ?? 0,
            mediaCounts.FirstOrDefault(x => x.Type == MediaType.Progress)?.Count ?? 0,
            mediaCounts.FirstOrDefault(x => x.Type == MediaType.After)?.Count ?? 0);

        var thumbnailUrl = await reportMedia.QueryAsNoTracking()
            .Where(m => m.ReportId == ev.ReportId && m.Type == MediaType.Image)
            .OrderBy(m => m.UploadedAt)
            .Select(m => m.ThumbnailUrl ?? m.Url)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        CommunityCleanupMyParticipationDto? myParticipation = null;
        if (currentUser.IsAuthenticated)
        {
            var mine = await participants.GetByEventAndUserAsync(ev.Id, currentUser.UserId, ct).ConfigureAwait(false);
            if (mine is not null)
                myParticipation = new CommunityCleanupMyParticipationDto(mine.Status, mine.JoinedAt, mine.Role);
        }

        return CommunityCleanupMapper.ToDetail(
            ev, report, leaderUser, leaderTeam,
            participantCount, mediaSummary, thumbnailUrl, myParticipation,
            isLeader: currentUser.IsAuthenticated && currentUser.UserId == ev.LeaderUserId);
    }
}
