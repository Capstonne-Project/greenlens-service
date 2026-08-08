using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetCompanyTeams;

/// <summary>
/// CompanyManager retrieves teams belonging to their company.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class GetCompanyTeamsQueryHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
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

        var query = teams.QueryAsNoTracking()
            .Include(t => t.LocalOffice)
            .Include(t => t.Members)
            .Where(t => t.CompanyId == companyId);

        if (request.IsActive.HasValue)
        {
            logger.LogInformation("Filtering teams by active status: {IsActive}", request.IsActive.Value);
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new CompanyTeamItem(
                t.Id,
                t.Name,
                t.TeamType,
                t.LocalOfficeId,
                t.LocalOffice != null ? t.LocalOffice.Name : null,
                t.IsActive,
                t.Members.Count,
                t.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        logger.LogInformation("Company teams found: {TotalCount}", totalCount);
        logger.LogInformation("Company teams items: {Items}", items);
        logger.LogInformation("Company teams pagination: {Pagination}", pagination);

        return new GetCompanyTeamsResponse(items, pagination);
    }
}
