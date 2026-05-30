using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetTeams;

public sealed class GetTeamsQueryHandler(
    IEnvironmentalTeamRepository teams,
    IReportAssignmentRepository assignments,
    ILogger<GetTeamsQueryHandler> logger)
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

        // Load active assignment counts for availability
        var activeStatuses = new[] { AssignmentStatus.Assigned, AssignmentStatus.InProgress };

        var teamList = await query
            .OrderBy(t => t.Name)
            .ToListAsync(ct).ConfigureAwait(false);

        // Get active assignments for all teams in one query
        var teamIds = teamList.Select(t => t.Id).ToList();
        var activeAssignments = await assignments.QueryAsNoTracking()
            .Where(a => teamIds.Contains(a.TeamId) && activeStatuses.Contains(a.Status))
            .Select(a => new { a.TeamId, a.ReportId })
            .ToListAsync(ct).ConfigureAwait(false);

        var busyMap = activeAssignments
            .GroupBy(a => a.TeamId)
            .ToDictionary(g => g.Key, g => g.First().ReportId);

        // Apply availability filter after computing status
        var filteredTeams = teamList.AsEnumerable();
        if (request.IsAvailable == true)
            filteredTeams = filteredTeams.Where(t => !busyMap.ContainsKey(t.Id));
        else if (request.IsAvailable == false)
            filteredTeams = filteredTeams.Where(t => busyMap.ContainsKey(t.Id));

        var materialized = filteredTeams.ToList();
        var totalCount = materialized.Count;
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var items = materialized
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t =>
            {
                var isBusy = busyMap.TryGetValue(t.Id, out var activeReportId);
                return new TeamItem(
                    t.Id, t.Name, t.TeamType, t.LocalOfficeId,
                    t.LocalOffice?.Name, t.IsActive, t.Members.Count, t.CreatedAt,
                    isBusy ? "Busy" : "Available",
                    isBusy ? activeReportId : null);
            })
            .ToList();

        return new GetTeamsResponse(items, pagination);
    }
}

