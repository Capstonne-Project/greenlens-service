using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetActiveCommunityCleanupByReportId;

public sealed class GetActiveCommunityCleanupByReportIdQueryHandler(
    ICommunityCleanupEventRepository events,
    ICommunityCleanupParticipantRepository participants,
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IUserRepository users,
    IEnvironmentalTeamRepository teams,
    ICurrentUser currentUser)
    : IRequestHandler<GetActiveCommunityCleanupByReportIdQuery, Result<CommunityCleanupEventDetailResponse?>>
{
    public async Task<Result<CommunityCleanupEventDetailResponse?>> Handle(
        GetActiveCommunityCleanupByReportIdQuery request, CancellationToken ct)
    {
        var ev = await events.GetActiveByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (ev is null)
            return Result<CommunityCleanupEventDetailResponse?>.Success(null);

        var detail = await CommunityCleanupDetailBuilder.BuildAsync(
            ev, reports, reportMedia, users, teams, participants, currentUser, ct).ConfigureAwait(false);

        return detail.IsSuccess
            ? Result<CommunityCleanupEventDetailResponse?>.Success(detail.Value)
            : Result<CommunityCleanupEventDetailResponse?>.Failure(detail.Error!);
    }
}
