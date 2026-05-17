using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetTeamById;

public sealed class GetTeamByIdQueryHandler(
    IEnvironmentalTeamRepository teams)
    : IRequestHandler<GetTeamByIdQuery, Result<TeamDetailResponse>>
{
    public async Task<Result<TeamDetailResponse>> Handle(
        GetTeamByIdQuery request, CancellationToken ct)
    {
        var team = await teams.QueryAsNoTracking()
            .Include(t => t.LocalOffice)
            .Include(t => t.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (team is null)
            return Errors.Organization.TeamNotFound;

        var members = team.Members.Select(m => new MemberInTeam(
            m.UserId, m.User?.FullName, m.User?.Email,
            m.IsLeader, m.JoinedAt)).ToList();

        return new TeamDetailResponse(
            team.Id, team.Name, team.TeamType, team.LocalOfficeId,
            team.LocalOffice?.Name, team.IsActive,
            members, team.CreatedAt, team.UpdatedAt);
    }
}
