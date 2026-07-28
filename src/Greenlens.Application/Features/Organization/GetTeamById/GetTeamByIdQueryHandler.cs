using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetTeamById;

/// <summary>
/// Returns team detail with members for Admin/LEO/DEO/CompanyManager callers.
/// </summary>
/// <remarks>Implements: BR-ORG-003.</remarks>
public sealed class GetTeamByIdQueryHandler(
    IEnvironmentalTeamRepository teams,
    IUserRepository users,
    ICompanyStaffRepository companyStaff,
    ILocalOfficeRepository localOffices,
    ICurrentUser currentUser,
    ILogger<GetTeamByIdQueryHandler> logger)
    : IRequestHandler<GetTeamByIdQuery, Result<TeamDetailResponse>>
{
    public async Task<Result<TeamDetailResponse>> Handle(
        GetTeamByIdQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting team by ID {Id}", request.Id);

        var (actor, actorError) = await TeamAccessAuthorization
            .ResolveActorAsync(users, currentUser, ct)
            .ConfigureAwait(false);

        if (actorError is not null)
            return actorError;

        var team = await teams.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("Team {Id} not found", request.Id);
            return Errors.Organization.TeamNotFound;
        }

        var accessError = await TeamAccessAuthorization.ValidateViewAccessAsync(
                team, actor!, companyStaff, localOffices, ct)
            .ConfigureAwait(false);

        if (accessError is not null)
        {
            logger.LogWarning(
                "User {UserId} denied access to team {TeamId}: {ErrorCode}",
                currentUser.UserId, request.Id, accessError.Code);
            return accessError;
        }

        var detail = await teams.QueryAsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(t => new TeamDetailResponse(
                t.Id,
                t.Name,
                t.TeamType,
                t.LocalOfficeId,
                t.LocalOffice != null ? t.LocalOffice.Name : null,
                t.IsActive,
                t.Members
                    .Select(m => new MemberInTeam(
                        m.UserId,
                        m.User != null ? m.User.FullName : null,
                        m.User != null ? m.User.Email : null,
                        m.User != null ? m.User.PhoneNumber : null,
                        m.User != null ? m.User.AvatarUrl : null,
                        m.IsLeader,
                        m.JoinedAt))
                    .ToList(),
                t.CreatedAt,
                t.UpdatedAt))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (detail is null)
        {
            logger.LogWarning("Team {Id} not found during projection", request.Id);
            return Errors.Organization.TeamNotFound;
        }

        logger.LogInformation("Lấy thông tin chi tiết đội ngũ thành công. Tên đội: {TeamName}", detail.Name);
        return detail;
    }
}
