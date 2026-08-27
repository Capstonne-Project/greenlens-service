using Greenlens.Domain.Entities;

namespace Greenlens.Application.Features.CommunityCleanup.Common;

internal static class CommunityCleanupMapper
{
    public static CommunityCleanupEventDetailResponse ToDetail(
        Domain.Entities.CommunityCleanupEvent ev,
        Report report,
        User leaderUser,
        EnvironmentalTeam leaderTeam,
        int participantCount,
        CommunityCleanupMediaSummaryDto mediaSummary,
        CommunityCleanupMediaDto media,
        string? thumbnailUrl,
        List<string> reportImageUrls,
        CommunityCleanupMyParticipationDto? myParticipation,
        bool isLeader,
        CommunityCleanupShareDto share)
    {
        return new CommunityCleanupEventDetailResponse(
            ev.Id,
            ev.ReportId,
            report.Code,
            ev.Status,
            ev.Title,
            ev.Description,
            report.Description,
            reportImageUrls,
            new CommunityCleanupLeaderDto(leaderUser.Id, leaderUser.FullName, leaderTeam.Id, leaderTeam.Name),
            ev.JoinOpensAt,
            ev.JoinClosesAt,
            ev.StartsAt,
            ev.EndsAt,
            ev.MaxParticipants,
            participantCount,
            Math.Max(0, ev.MaxParticipants - participantCount),
            ev.ProgressPercent,
            ev.ProgressNote,
            ev.MeetingNote,
            ev.MeetingLatitude,
            ev.MeetingLongitude,
            report.Latitude,
            report.Longitude,
            report.Address,
            report.Category.NameVi,
            report.Severity,
            thumbnailUrl,
            myParticipation,
            isLeader,
            mediaSummary,
            media,
            share);
    }
}
