using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetCompanyStaff;

/// <summary>
/// Returns paginated list of company staff with their team assignment info.
/// </summary>
public sealed class GetCompanyStaffQueryHandler(
    ICompanyStaffRepository companyStaffRepo,
    ITeamMemberRepository teamMembers,
    IEnvironmentalTeamRepository teams,
    ICurrentUser currentUser)
    : IRequestHandler<GetCompanyStaffQuery, Result<GetCompanyStaffResponse>>
{
    public async Task<Result<GetCompanyStaffResponse>> Handle(
        GetCompanyStaffQuery request,
        CancellationToken ct)
    {
        // ── 1. Resolve CM's company ──
        var cmStaff = await companyStaffRepo.GetByUserIdAsync(currentUser.UserId, ct)
            .ConfigureAwait(false);

        if (cmStaff is null)
            return Errors.Organization.NotCompanyManager;

        var companyId = cmStaff.CompanyId;

        // ── 2. Query staff ──
        var query = companyStaffRepo.QueryAsNoTracking()
            .Include(s => s.User)
            .Where(s => s.CompanyId == companyId);

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var staffList = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // ── 3. Get team membership info for these staff ──
        var staffUserIds = staffList.Select(s => s.UserId).ToList();

        // Get company teams
        var companyTeams = await teams.QueryAsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var companyTeamIds = companyTeams.Select(t => t.Id).ToHashSet();

        // Get team memberships for staff in company teams
        var memberships = await teamMembers.QueryAsNoTracking()
            .Where(tm => staffUserIds.Contains(tm.UserId) && companyTeamIds.Contains(tm.TeamId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var membershipLookup = memberships
            .GroupBy(tm => tm.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var teamLookup = companyTeams.ToDictionary(t => t.Id, t => t.Name);

        // ── 4. Project response ──
        var items = staffList.Select(s =>
        {
            var hasMembership = membershipLookup.TryGetValue(s.UserId, out var membership);
            return new CompanyStaffItem(
                s.UserId,
                s.User!.Email,
                s.User.FullName,
                s.Position,
                s.IsActive,
                hasMembership ? teamLookup.GetValueOrDefault(membership!.TeamId) : null,
                hasMembership ? membership!.TeamId : null,
                s.CreatedAt);
        }).ToList();

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        return new GetCompanyStaffResponse(items, pagination);
    }
}
