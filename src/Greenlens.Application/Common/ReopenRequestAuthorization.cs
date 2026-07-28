using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common;

/// <summary>Shared BR-ORG-012 scope checks for LEO reopen review actions.</summary>
public static class ReopenRequestAuthorization
{
    public static async Task<Error?> ValidateLeoScopeAsync(
        Report report,
        IUserRepository users,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        if (user.Role is UserRole.Admin)
            return null;

        if (user.Role != UserRole.LEO)
            return Errors.Reports.ReopenReviewForbidden;

        if (!user.LocalOfficeId.HasValue)
            return Errors.Organization.OfficeNotFound;

        if (report.AssignedOfficeId != user.LocalOfficeId)
            return Errors.Reports.OutsideJurisdiction;

        return null;
    }
}
