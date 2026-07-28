using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.CreateCommunityCleanup;

/// <remarks>
/// Draft rules: BR-CMU-001 (Verified report only), BR-CMU-002 (Leader = active Cleaner in a Cleanup team),
/// BR-CMU-003 (one active event per report; replaces AssignTeam/DispatchToCompany).
/// </remarks>
public sealed class CreateCommunityCleanupCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    ICommunityCleanupEventRepository events,
    ICommunityCleanupParticipantRepository participants,
    ITeamMemberRepository teamMembers,
    IEnvironmentalTeamRepository teams,
    IUserRepository users,
    IReportMediaRepository reportMedia,
    IReportStatusHistoryRepository statusHistory,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<CreateCommunityCleanupCommandHandler> logger)
    : IRequestHandler<CreateCommunityCleanupCommand, Result<CommunityCleanupEventDetailResponse>>
{
    public async Task<Result<CommunityCleanupEventDetailResponse>> Handle(
        CreateCommunityCleanupCommand request, CancellationToken ct)
    {
        var report = await reports.Query()
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct)
            .ConfigureAwait(false);

        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (report.Status != ReportStatus.Verified)
        {
            logger.LogWarning("Report {ReportId} is not verified", request.ReportId);
            return Errors.CommunityCleanup.ReportNotVerified;
        }

        var activeEvent = await events.GetActiveByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (activeEvent is not null)
        {
            logger.LogWarning("Report {ReportId} already has an active community cleanup event", request.ReportId);
            return Errors.CommunityCleanup.CommunityAlreadyActive;
        }

        var existingAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        var hasActiveAssignment = existingAssignments.Any(a => a.Status != AssignmentStatus.Declined)
            || report.AssignedCompanyId.HasValue;
        if (hasActiveAssignment)
        {
            logger.LogWarning("Report {ReportId} already has an active team/company assignment", request.ReportId);
            return Errors.CommunityCleanup.ReportHasActiveAssignment;
        }

        var leaderUser = await users.GetByIdAsync(request.LeaderUserId, ct).ConfigureAwait(false);
        if (leaderUser is null || leaderUser.Role != UserRole.Cleaner)
        {
            logger.LogWarning("Leader {LeaderUserId} is not a Cleaner", request.LeaderUserId);
            return Errors.CommunityCleanup.LeaderNotCleaner;
        }

        var leaderMember = await teamMembers.GetByUserIdAsync(request.LeaderUserId, ct).ConfigureAwait(false);
        if (leaderMember is null)
        {
            logger.LogWarning("Leader {LeaderUserId} is not in any team", request.LeaderUserId);
            return Errors.CommunityCleanup.LeaderNotInCleanupTeam;
        }

        var leaderTeam = await teams.GetByIdAsync(leaderMember.TeamId, ct).ConfigureAwait(false);
        if (leaderTeam is null || leaderTeam.TeamType != TeamType.Cleanup || !leaderTeam.IsActive)
        {
            logger.LogWarning("Leader {LeaderUserId} team {TeamId} is not an active Cleanup team", request.LeaderUserId, leaderMember.TeamId);
            return Errors.CommunityCleanup.LeaderNotInCleanupTeam;
        }

        var ev = Domain.Entities.CommunityCleanupEvent.Create(
            report.Id,
            currentUser.UserId,
            request.LeaderUserId,
            leaderTeam.Id,
            request.Title,
            request.Description,
            request.StartsAt,
            request.EndsAt,
            request.JoinClosesAt,
            request.MaxParticipants,
            request.MeetingNote,
            request.MeetingLatitude,
            request.MeetingLongitude);

        events.Add(ev);
        participants.Add(CommunityCleanupParticipant.Create(ev.Id, request.LeaderUserId, CommunityCleanupParticipantRole.Leader));

        report.StartCommunityCleanup(currentUser.UserId);
        statusHistory.Add(ReportStatusHistory.Create(
            report.Id, ReportStatus.Verified, ReportStatus.InProgress, currentUser.UserId));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "LEO {LeoId} opened community cleanup {EventId} on report {ReportId} with Leader {LeaderUserId}",
            currentUser.UserId, ev.Id, report.Id, request.LeaderUserId);

        var thumbnailUrl = await reportMedia.QueryAsNoTracking()
            .Where(m => m.ReportId == report.Id && m.Type == MediaType.Image)
            .OrderBy(m => m.UploadedAt)
            .Select(m => m.ThumbnailUrl ?? m.Url)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return CommunityCleanupMapper.ToDetail(
            ev, report, leaderUser, leaderTeam,
            participantCount: 1,
            mediaSummary: new CommunityCleanupMediaSummaryDto(0, 0, 0),
            thumbnailUrl: thumbnailUrl,
            myParticipation: null,
            isLeader: currentUser.UserId == ev.LeaderUserId);
    }
}
