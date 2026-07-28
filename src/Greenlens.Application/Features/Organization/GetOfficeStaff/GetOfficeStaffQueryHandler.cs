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
/// <remarks>Implements: BR-ORG-003, BR-ORG-020.</remarks>
public sealed class GetOfficeStaffQueryHandler(
    IUserRepository userRepo,
    ITeamMemberRepository teamMemberRepo,
    IEnvironmentalTeamRepository teams,
    ICurrentUser currentUser,
    ILogger<GetOfficeStaffQueryHandler> logger) : IRequestHandler<GetOfficeStaffQuery, Result<GetOfficeStaffResponse>>
{
    public async Task<Result<GetOfficeStaffResponse>> Handle(
        GetOfficeStaffQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting office staff for user {UserId}", currentUser.UserId);

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
        var teamMembers = teamMemberRepo.QueryAsNoTracking();

        var staffQuery = userRepo.QueryAsNoTracking()
            .Where(u => u.LocalOfficeId == officeId)
            .Where(u => u.Role == UserRole.Cleaner || u.Role == UserRole.Inspector);

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

        if (request.HasTeam == true)
        {
            logger.LogInformation("Filtering staff by has team: true");
            staffQuery = staffQuery.Where(u => teamMembers.Any(tm => tm.UserId == u.Id));
        }
        else if (request.HasTeam == false)
        {
            logger.LogInformation("Filtering staff by has team: false");
            staffQuery = staffQuery.Where(u => !teamMembers.Any(tm => tm.UserId == u.Id));
        }

        var totalCount = await staffQuery.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var users = await staffQuery
            .OrderBy(u => u.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (users.Count == 0)
        {
            return new GetOfficeStaffResponse([], pagination);
        }

        var userIds = users.Select(u => u.Id).ToList();
        var memberships = await teamMembers
            .Where(tm => userIds.Contains(tm.UserId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var membershipByUser = memberships
            .GroupBy(m => m.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var teamIds = membershipByUser.Values.Select(m => m.TeamId).Distinct().ToList();
        var teamNameById = teamIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await teams.QueryAsNoTracking()
                .Where(t => teamIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Name, ct)
                .ConfigureAwait(false);

        var items = users.Select(u =>
        {
            membershipByUser.TryGetValue(u.Id, out var membership);
            string? teamName = membership is not null && teamNameById.TryGetValue(membership.TeamId, out var name)
                ? name
                : null;

            return new OfficeStaffItem(
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.AvatarUrl,
                u.Role,
                membership?.TeamId,
                teamName,
                membership?.IsLeader ?? false,
                u.CreatedAt);
        }).ToList();

        logger.LogInformation("LEO {LeoId} fetched {Count} staff for office {OfficeId}",
            currentUser.UserId, items.Count, officeId);

        return new GetOfficeStaffResponse(items, pagination);
    }
}
