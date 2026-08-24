using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetTeams;

/// <summary>
/// Lists community teams scoped to the caller: LEO → own office, DEO → department offices, Admin → optional filter.
/// </summary>
/// <remarks>Implements: BR-ORG-003, BR-CLN-005, BR-OFF-014.</remarks>
public sealed class GetTeamsQueryHandler(
    IEnvironmentalTeamRepository teams,
    IReportAssignmentRepository assignments,
    IReportRepository reports,
    IReportWasteTagRepository reportWasteTags,
    IUserRepository users,
    ILocalOfficeRepository localOffices,
    ICurrentUser currentUser,
    ILogger<GetTeamsQueryHandler> logger)
    : IRequestHandler<GetTeamsQuery, Result<GetTeamsResponse>>
{
    public async Task<Result<GetTeamsResponse>> Handle(
        GetTeamsQuery request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("User not found for teams list: {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        logger.LogInformation("Getting community teams for user {UserId}", currentUser.UserId);

        Guid? leoOfficeId = user.Role == UserRole.LEO ? user.LocalOfficeId : null;

        var prioritizeResult = await TeamListPrioritizeHelper.ResolvePrioritizeTagIdsForLeoAsync(
                request.ReportId,
                request.WasteTagIds,
                reports,
                reportWasteTags,
                leoOfficeId,
                ct)
            .ConfigureAwait(false);

        if (!prioritizeResult.IsSuccess)
            return prioritizeResult.Error!;

        var prioritizeIds = prioritizeResult.Value!;
        var filterTagIds = request.WasteTagIds?.Distinct().ToList() ?? [];

        var query = teams.QueryAsNoTracking()
            .Include(t => t.LocalOffice)
            .Include(t => t.Members)
            .Include(t => t.WasteTags).ThenInclude(tw => tw.WasteTag)
            .Where(t => t.CompanyId == null)
            .AsQueryable();

        if (user.Role == UserRole.LEO)
        {
            if (!user.LocalOfficeId.HasValue)
            {
                logger.LogWarning("LEO {UserId} has no office", currentUser.UserId);
                return Errors.Organization.OfficerNoOffice;
            }

            query = query.Where(t => t.LocalOfficeId == user.LocalOfficeId.Value);
        }
        else if (user.Role == UserRole.DEO)
        {
            if (!user.DepartmentId.HasValue)
            {
                logger.LogWarning("DEO {UserId} has no department", currentUser.UserId);
                return Errors.Organization.DepartmentNotFound;
            }

            var officeIds = await localOffices.QueryAsNoTracking()
                .Where(o => o.DepartmentId == user.DepartmentId.Value)
                .Select(o => o.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            query = query.Where(t =>
                t.LocalOfficeId != null && officeIds.Contains(t.LocalOfficeId.Value));
        }
        else if (request.LocalOfficeId.HasValue)
        {
            query = query.Where(t => t.LocalOfficeId == request.LocalOfficeId.Value);
        }

        if (request.TeamType.HasValue)
            query = query.Where(t => t.TeamType == request.TeamType.Value);

        if (request.IsActive.HasValue)
            query = query.Where(t => t.IsActive == request.IsActive.Value);

        if (filterTagIds.Count > 0)
        {
            query = query.Where(t =>
                t.WasteTags.Any(tw => filterTagIds.Contains(tw.WasteTagId)));
        }

        var activeStatuses = new[] { AssignmentStatus.Assigned, AssignmentStatus.InProgress };

        var teamList = await query.ToListAsync(ct).ConfigureAwait(false);

        var teamIds = teamList.Select(t => t.Id).ToList();
        var activeAssignments = await assignments.QueryAsNoTracking()
            .Where(a => teamIds.Contains(a.TeamId) && activeStatuses.Contains(a.Status))
            .Select(a => new { a.TeamId, a.ReportId })
            .ToListAsync(ct).ConfigureAwait(false);

        var busyMap = activeAssignments
            .GroupBy(a => a.TeamId)
            .ToDictionary(g => g.Key, g => g.First().ReportId);

        IEnumerable<Domain.Entities.EnvironmentalTeam> filteredTeams = teamList;
        if (request.IsAvailable == true)
            filteredTeams = filteredTeams.Where(t => !busyMap.ContainsKey(t.Id));
        else if (request.IsAvailable == false)
            filteredTeams = filteredTeams.Where(t => busyMap.ContainsKey(t.Id));

        var ordered = prioritizeIds.Count > 0
            ? filteredTeams
                .OrderByDescending(t => TeamWasteTagService.CountMatchingTags(t, prioritizeIds))
                .ThenBy(t => t.Name)
            : filteredTeams.OrderBy(t => t.Name);

        var materialized = ordered.ToList();
        var totalCount = materialized.Count;
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var items = materialized
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t =>
            {
                var isBusy = busyMap.TryGetValue(t.Id, out var activeReportId);
                var matchCount = prioritizeIds.Count > 0
                    ? TeamWasteTagService.CountMatchingTags(t, prioritizeIds)
                    : (int?)null;

                return new TeamItem(
                    t.Id,
                    t.Name,
                    t.TeamType,
                    t.LocalOfficeId,
                    t.LocalOffice?.Name,
                    t.IsActive,
                    t.Members.Count,
                    t.CreatedAt,
                    isBusy ? "Busy" : "Available",
                    isBusy ? activeReportId : null,
                    TeamWasteTagService.MapTags(t),
                    matchCount);
            })
            .ToList();

        logger.LogInformation("Lấy danh sách đội ngũ thành công. Số lượng: {Count}", items.Count);
        return new GetTeamsResponse(items, pagination);
    }
}
