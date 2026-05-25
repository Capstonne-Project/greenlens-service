using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.GetTeamById;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetMyTeamProfile;

public sealed class GetMyTeamProfileQueryHandler(
    IEnvironmentalTeamRepository teams,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyTeamProfileQuery, Result<TeamDetailResponse>>
{
    public async Task<Result<TeamDetailResponse>> Handle(
        GetMyTeamProfileQuery request, CancellationToken ct)
    {
        var team = await teams.QueryAsNoTracking()
            .Include(t => t.LocalOffice)
            .Include(t => t.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(
                t => t.Members.Any(m => m.UserId == currentUser.UserId), ct)
            .ConfigureAwait(false);

        if (team is null)
            return Errors.Organization.TeamNotFound;

        var members = team.Members.Select(m => new MemberInTeam(
            m.UserId, m.User?.FullName, m.User?.Email, m.User?.PhoneNumber,
            m.User?.AvatarUrl, m.IsLeader, m.JoinedAt)).ToList();

        return new TeamDetailResponse(
            team.Id, team.Name, team.TeamType, team.LocalOfficeId,
            team.LocalOffice?.Name, team.IsActive,
            members, team.CreatedAt, team.UpdatedAt);
    }
}
