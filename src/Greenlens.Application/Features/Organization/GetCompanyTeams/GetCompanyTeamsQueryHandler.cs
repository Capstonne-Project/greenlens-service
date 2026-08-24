using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetCompanyTeams;

/// <summary>
/// CompanyManager retrieves teams belonging to their company.
/// </summary>
/// <remarks>Implements: BR-CMP-004, BR-CLN-005, BR-OFF-014.</remarks>
public sealed class GetCompanyTeamsQueryHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    IReportRepository reports,
    IReportWasteTagRepository reportWasteTags,
    ICurrentUser currentUser,
    ILogger<GetCompanyTeamsQueryHandler> logger) : IRequestHandler<GetCompanyTeamsQuery, Result<GetCompanyTeamsResponse>>
{
    public async Task<Result<GetCompanyTeamsResponse>> Handle(
        GetCompanyTeamsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting company teams for user {UserId}", currentUser.UserId);

        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (staff is null || !staff.IsActive)
        {
            logger.LogWarning("Company manager not found or inactive for user ID {UserId}", currentUser.UserId);
            return Errors.Organization.NotCompanyManager;
        }

        var companyId = staff.CompanyId;

        var prioritizeResult = await TeamListPrioritizeHelper.ResolvePrioritizeTagIdsForCompanyAsync(
                request.ReportId,
                request.WasteTagIds,
                reports,
                reportWasteTags,
                companyId,
                cancellationToken)
            .ConfigureAwait(false);

        if (!prioritizeResult.IsSuccess)
            return prioritizeResult.Error!;

        var prioritizeIds = prioritizeResult.Value!;
        var filterTagIds = request.WasteTagIds?.Distinct().ToList() ?? [];

        var query = teams.QueryAsNoTracking()
            .Include(t => t.LocalOffice)
            .Include(t => t.Members)
            .Include(t => t.WasteTags).ThenInclude(tw => tw.WasteTag)
            .Where(t => t.CompanyId == companyId);

        if (request.IsActive.HasValue)
            query = query.Where(t => t.IsActive == request.IsActive.Value);

        if (filterTagIds.Count > 0)
        {
            query = query.Where(t =>
                t.WasteTags.Any(tw => filterTagIds.Contains(tw.WasteTagId)));
        }

        var teamList = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        var ordered = (prioritizeIds.Count > 0
            ? teamList
                .OrderByDescending(t => TeamWasteTagService.CountMatchingTags(t, prioritizeIds))
                .ThenBy(t => t.Name)
            : teamList.OrderByDescending(t => t.CreatedAt))
            .ToList();

        var totalCount = ordered.Count;
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var items = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t =>
            {
                var matchCount = prioritizeIds.Count > 0
                    ? TeamWasteTagService.CountMatchingTags(t, prioritizeIds)
                    : (int?)null;

                return new CompanyTeamItem(
                    t.Id,
                    t.Name,
                    t.TeamType,
                    t.LocalOfficeId,
                    t.LocalOffice?.Name,
                    t.IsActive,
                    t.Members.Count,
                    t.CreatedAt,
                    TeamWasteTagService.MapTags(t),
                    matchCount);
            })
            .ToList();

        logger.LogInformation("Company teams found: {TotalCount}", totalCount);

        return new GetCompanyTeamsResponse(items, pagination);
    }
}
