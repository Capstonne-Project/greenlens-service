using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetTeams;

public sealed class GetTeamsQueryHandler(
    IEnvironmentalTeamRepository teams)
    : IRequestHandler<GetTeamsQuery, Result<GetTeamsResponse>>
{
    public async Task<Result<GetTeamsResponse>> Handle(
        GetTeamsQuery request, CancellationToken ct)
    {
        var query = teams.QueryAsNoTracking()
            .Include(t => t.LocalOffice)
            .Include(t => t.Members)
            .AsQueryable();

        if (request.LocalOfficeId.HasValue)
            query = query.Where(t => t.LocalOfficeId == request.LocalOfficeId.Value);
        if (request.TeamType.HasValue)
            query = query.Where(t => t.TeamType == request.TeamType.Value);
        if (request.IsActive.HasValue)
            query = query.Where(t => t.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TeamItem(
                t.Id, t.Name, t.TeamType, t.LocalOfficeId,
                t.LocalOffice != null ? t.LocalOffice.Name : null,
                t.IsActive, t.Members.Count, t.CreatedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        return new GetTeamsResponse(items, totalCount, request.Page, request.PageSize);
    }
}
