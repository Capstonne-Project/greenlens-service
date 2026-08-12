using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.Common;

/// <summary>Resolved department scope for DEO dashboard queries.</summary>
public sealed record DepartmentScope(Guid DepartmentId, string DepartmentName);

/// <summary>Resolves the caller's department for province-scoped dashboard queries.</summary>
public static class DepartmentContextResolver
{
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
}
