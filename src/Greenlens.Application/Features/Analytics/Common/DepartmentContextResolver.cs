using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.Common;

/// <summary>Resolved department scope for DEO dashboard queries.</summary>
public sealed record DepartmentScope(Guid DepartmentId, string DepartmentName);

/// <summary>Resolves the caller's department for province-scoped dashboard queries.</summary>
public static class DepartmentContextResolver
{
    /// <summary>
    /// Loads department from the authenticated user's profile — never from query/route params.
    /// </summary>
    public static async Task<Result<DepartmentScope>> ResolveAsync(
        IUserRepository users,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var info = await users.QueryAsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new
            {
                DepartmentId = u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.Name : null
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (info?.DepartmentId is null || info.DepartmentId == Guid.Empty)
            return Errors.Organization.DepartmentNotFound;

        return new DepartmentScope(info.DepartmentId.Value, info.DepartmentName ?? string.Empty);
    }

    public static IQueryable<Report> ApplyDepartmentScope(IQueryable<Report> query, Guid departmentId) =>
        query.Where(r => r.AssignedDepartmentId == departmentId);

    /// <summary>DEO may only access resources belonging to their own department (Admin bypasses).</summary>
    public static Error? ValidateDeoDepartmentAccess(User actor, Guid resourceDepartmentId)
    {
        if (actor.Role != UserRole.DEO)
            return null;

        if (!actor.DepartmentId.HasValue)
            return Errors.Organization.DepartmentNotFound;

        if (resourceDepartmentId != actor.DepartmentId)
            return Errors.Reports.OutsideJurisdiction;

        return null;
    }

    /// <summary>When DEO lists org data, force scope to their department (Admin bypasses).</summary>
    public static Guid? ResolveDepartmentFilter(User actor, Guid? requestedDepartmentId)
    {
        if (actor.Role != UserRole.DEO)
            return requestedDepartmentId;

        return actor.DepartmentId;
    }
}