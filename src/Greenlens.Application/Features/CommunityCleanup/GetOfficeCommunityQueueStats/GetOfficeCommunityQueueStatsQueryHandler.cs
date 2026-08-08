using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.CommunityCleanup.GetOfficeCommunityQueueStats;

public sealed class GetOfficeCommunityQueueStatsQueryHandler(
    ICommunityCleanupEventRepository events,
    IReportMediaRepository reportMedia,
    IUserRepository users,
    ICurrentUser currentUser)
    : IRequestHandler<GetOfficeCommunityQueueStatsQuery, Result<CommunityCleanupQueueStatsResponse>>
{
    public async Task<Result<CommunityCleanupQueueStatsResponse>> Handle(
        GetOfficeCommunityQueueStatsQuery request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        var query = events.QueryAsNoTracking().AsQueryable();

        if (user.Role == UserRole.LEO && user.LocalOfficeId.HasValue)
            query = query.Where(e => e.Report!.AssignedOfficeId == user.LocalOfficeId.Value);

        var countsByStatus = await query
            .GroupBy(e => e.Status)
            .Select(g => new CommunityCleanupStatusCountDto(g.Key, g.Count()))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var totalParticipants = await query
            .SelectMany(e => e.Participants)
            .Where(p => p.Status != CommunityCleanupParticipantStatus.Withdrawn
                && p.Status != CommunityCleanupParticipantStatus.NoShow)
            .CountAsync(ct)
            .ConfigureAwait(false);

        var reportIds = await query.Select(e => e.ReportId).ToListAsync(ct).ConfigureAwait(false);

        var totalMediaCount = await reportMedia.QueryAsNoTracking()
            .Where(m => reportIds.Contains(m.ReportId)
                && (m.Type == MediaType.Before || m.Type == MediaType.Progress || m.Type == MediaType.After))
            .CountAsync(ct)
            .ConfigureAwait(false);

        return new CommunityCleanupQueueStatsResponse(countsByStatus, totalParticipants, totalMediaCount);
    }
}
