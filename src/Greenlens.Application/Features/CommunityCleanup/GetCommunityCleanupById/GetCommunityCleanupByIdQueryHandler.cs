using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
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

        return await CommunityCleanupDetailBuilder.BuildAsync(
            ev, reports, reportMedia, users, teams, participants, currentUser, ct).ConfigureAwait(false);
    }
}
