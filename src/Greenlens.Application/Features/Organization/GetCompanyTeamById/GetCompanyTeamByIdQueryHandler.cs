using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Application.Features.Organization.GetTeamById;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetCompanyTeamById;

/// <summary>
/// Returns company team detail with members for CompanyManager (own company) or Admin.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class GetCompanyTeamByIdQueryHandler(
    ICompanyStaffRepository companyStaff,
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    ILogger<GetCompanyTeamByIdQueryHandler> logger)
    : IRequestHandler<GetCompanyTeamByIdQuery, Result<CompanyTeamDetailResponse>>
{
    public async Task<Result<CompanyTeamDetailResponse>> Handle(
        GetCompanyTeamByIdQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting company team {TeamId} for user {UserId}",
            request.TeamId, currentUser.UserId);

        var team = await teams.QueryAsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TeamId, ct)
            .ConfigureAwait(false);

        if (team is null)
        {
            logger.LogWarning("Team {TeamId} not found", request.TeamId);
            return Errors.Organization.TeamNotFound;
        }

        if (!team.IsCompanyTeam || !team.CompanyId.HasValue)
        {
            logger.LogWarning("Team {TeamId} is not a company team", request.TeamId);
            return Errors.Organization.TeamNotInCompany;
        }

        if (currentUser.Role != UserRole.Admin.ToString())
        {
            var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct)
                .ConfigureAwait(false);

            if (staff is null || !staff.IsActive)
            {
                logger.LogWarning("Company manager not found or inactive for user {UserId}",
                    currentUser.UserId);
                return Errors.Organization.NotCompanyManager;
            }

            if (team.CompanyId != staff.CompanyId)
            {
                logger.LogWarning(
                    "Team {TeamId} is not in company {CompanyId}",
                    request.TeamId, staff.CompanyId);
                return Errors.Organization.TeamNotInCompany;
            }
        }

        var members = await teamMembers.QueryAsNoTracking()
            .Where(m => m.TeamId == request.TeamId)
            .Select(m => new MemberInTeam(
                m.UserId,
                m.User != null ? m.User.FullName : null,
                m.User != null ? m.User.Email : null,
                m.User != null ? m.User.PhoneNumber : null,
                m.User != null ? m.User.AvatarUrl : null,
                m.IsLeader,
                m.JoinedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var teamWithTags = await teams.QueryAsNoTracking()
            .Include(t => t.WasteTags).ThenInclude(tw => tw.WasteTag)
            .FirstOrDefaultAsync(t => t.Id == request.TeamId, ct)
            .ConfigureAwait(false);

        var wasteTags = teamWithTags is not null
            ? TeamWasteTagService.MapTags(teamWithTags)
            : [];

        logger.LogInformation(
            "Company team detail fetched: {TeamName} ({MemberCount} members)",
            team.Name,
            members.Count);

        return new CompanyTeamDetailResponse(
            team.Id,
            team.Name,
            team.TeamType,
            team.CompanyId.Value,
            team.IsActive,
            members.Count,
            members,
            wasteTags,
            team.CreatedAt,
            team.UpdatedAt);
    }
}
