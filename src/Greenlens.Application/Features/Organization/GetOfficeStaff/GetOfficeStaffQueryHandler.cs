using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetOfficeStaff;

/// <summary>
/// Returns paginated list of Cleaner/Inspector staff in the LEO's LocalOffice.
/// Includes team membership info (left join) so LEO can see who is unassigned.
/// </summary>
public sealed class GetOfficeStaffQueryHandler(
    IUserRepository userRepo,
    ITeamMemberRepository teamMemberRepo,
    ICurrentUser currentUser,
    ILogger<GetOfficeStaffQueryHandler> logger) : IRequestHandler<GetOfficeStaffQuery, Result<GetOfficeStaffResponse>>
{
    public async Task<Result<GetOfficeStaffResponse>> Handle(
        GetOfficeStaffQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting office staff for user {UserId}", currentUser.UserId);

        // ── 1. Get LEO's office ──
        var leo = await userRepo.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leo is null)
        {
            logger.LogWarning("User not found for ID {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        if (!leo.LocalOfficeId.HasValue)
        {
            logger.LogWarning("User {UserId} has no office", currentUser.UserId);
            return Errors.Organization.OfficerNoOffice;
        }

        var officeId = leo.LocalOfficeId.Value;

        // ── 2. Query staff in the office (Cleaner + Inspector only) ──
        var staffQuery = userRepo.QueryAsNoTracking()
            .Where(u => u.LocalOfficeId == officeId)
            .Where(u => u.Role == UserRole.Cleaner || u.Role == UserRole.Inspector);

        // ── 3. Filters ──
        if (request.RoleFilter.HasValue)
        {
            logger.LogInformation("Filtering staff by role: {Role}", request.RoleFilter.Value);
            staffQuery = staffQuery.Where(u => u.Role == request.RoleFilter.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            logger.LogInformation("Filtering staff by search: {Search}", request.Search);
            var keyword = request.Search.Trim().ToLower();
            staffQuery = staffQuery.Where(u =>
                u.FullName.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword));
        }

        // ── 4. Left join with TeamMember to get team info ──
        var joinedQuery = from u in staffQuery
                          join tm in teamMemberRepo.QueryAsNoTracking()
                              on u.Id equals tm.UserId into tmGroup
                          from tm in tmGroup.DefaultIfEmpty()
                          select new { User = u, TeamMember = tm };

        // ── 5. Filter by HasTeam ──
        if (request.HasTeam == true)
        {
            logger.LogInformation("Filtering staff by has team: true");
            joinedQuery = joinedQuery.Where(x => x.TeamMember != null);
        }
        else if (request.HasTeam == false)
        {
            logger.LogInformation("Filtering staff by has team: false");
            joinedQuery = joinedQuery.Where(x => x.TeamMember == null);
        }

        var totalCount = await joinedQuery.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        // ── 6. Project + paginate ──
        var items = await joinedQuery
            .OrderBy(x => x.User.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new OfficeStaffItem(
                x.User.Id,
                x.User.FullName,
                x.User.Email,
                x.User.PhoneNumber,
                x.User.AvatarUrl,
                x.User.Role,
                x.TeamMember != null ? x.TeamMember.TeamId : null,
                x.TeamMember != null && x.TeamMember.Team != null ? x.TeamMember.Team.Name : null,
                x.TeamMember != null && x.TeamMember.IsLeader,
                x.User.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("LEO {LeoId} fetched {Count} staff for office {OfficeId}",
            currentUser.UserId, items.Count, officeId);

        return new GetOfficeStaffResponse(items, pagination);
    }
}
