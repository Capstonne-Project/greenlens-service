using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetCompanyTeams;

/// <summary>
/// CompanyManager retrieves teams belonging to their company.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class GetCompanyTeamsQueryHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    ICurrentUser currentUser) : IRequestHandler<GetCompanyTeamsQuery, Result<GetCompanyTeamsResponse>>
{
    public async Task<Result<GetCompanyTeamsResponse>> Handle(
        GetCompanyTeamsQuery request,
        CancellationToken cancellationToken)
    {
        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (staff is null)
            return Errors.Organization.NotCompanyManager;

        var companyId = staff.CompanyId;

        var query = teams.QueryAsNoTracking()
            .Include(t => t.LocalOffice)
            .Include(t => t.Members)
            .Where(t => t.CompanyId == companyId);

        if (request.IsActive.HasValue)
            query = query.Where(t => t.IsActive == request.IsActive.Value);

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

        return new GetCompanyTeamsResponse(items, pagination);
    }
}
