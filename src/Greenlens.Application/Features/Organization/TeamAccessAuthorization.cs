using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization;

/// <summary>Scope checks for community/company team read and LEO manage actions.</summary>
/// <remarks>Implements: BR-ORG-003.</remarks>
internal static class TeamAccessAuthorization
{
    public static async Task<(User? Actor, Error? Error)> ResolveActorAsync(
        IUserRepository users,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var actor = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        return actor is null
            ? (null, Errors.Users.UserNotFound)
            : (actor, null);
    }

    public static async Task<Error?> ValidateViewAccessAsync(
        EnvironmentalTeam team,
        User actor,
        ICompanyStaffRepository companyStaff,
        ILocalOfficeRepository localOffices,
        CancellationToken ct)
    {
        if (actor.Role == UserRole.Admin)
            return null;

        return actor.Role switch
        {
            UserRole.LEO => ValidateLeoCommunityTeam(team, actor),
            UserRole.DEO => await ValidateDeoCommunityTeamAsync(team, actor, localOffices, ct)
                .ConfigureAwait(false),
            UserRole.CompanyManager => await ValidateCompanyManagerTeamAsync(
                    team, actor, companyStaff, ct)
                .ConfigureAwait(false),
            _ => Errors.Auth.Forbidden
        };
    }

    public static Error? ValidateLeoManageCommunityTeam(EnvironmentalTeam team, User leo)
    {
        if (leo.Role != UserRole.LEO && leo.Role != UserRole.Admin)
            return Errors.Auth.Forbidden;

        if (leo.Role == UserRole.Admin)
            return null;

        return ValidateLeoCommunityTeam(team, leo);
    }

    private static Error? ValidateLeoCommunityTeam(EnvironmentalTeam team, User leo)
    {
        if (!leo.LocalOfficeId.HasValue)
            return Errors.Organization.OfficerNoOffice;

        if (team.IsCompanyTeam)
            return Errors.Organization.TeamNotInOffice;

        if (team.LocalOfficeId != leo.LocalOfficeId)
            return Errors.Organization.TeamNotInOffice;

        return null;
    }

    private static async Task<Error?> ValidateDeoCommunityTeamAsync(
        EnvironmentalTeam team,
        User deo,
        ILocalOfficeRepository localOffices,
        CancellationToken ct)
    {
        if (team.IsCompanyTeam)
            return Errors.Organization.TeamNotInOffice;

        if (!deo.DepartmentId.HasValue || !team.LocalOfficeId.HasValue)
            return Errors.Organization.TeamNotInOffice;

        var officeDepartmentId = await localOffices.QueryAsNoTracking()
            .Where(o => o.Id == team.LocalOfficeId.Value)
            .Select(o => (Guid?)o.DepartmentId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (officeDepartmentId != deo.DepartmentId)
            return Errors.Organization.TeamNotInOffice;

        return null;
    }

    private static async Task<Error?> ValidateCompanyManagerTeamAsync(
        EnvironmentalTeam team,
        User actor,
        ICompanyStaffRepository companyStaff,
        CancellationToken ct)
    {
        if (!team.IsCompanyTeam)
            return Errors.Organization.TeamNotInCompany;

        var staff = await companyStaff.GetByUserIdAsync(actor.Id, ct).ConfigureAwait(false);
        if (staff is null)
            return Errors.Organization.NotCompanyManager;

        if (team.CompanyId != staff.CompanyId)
            return Errors.Organization.TeamNotInCompany;

        return null;
    }
}
