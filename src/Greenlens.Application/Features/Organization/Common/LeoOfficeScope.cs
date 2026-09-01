using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.Common;

/// <summary>Resolves the local office and ward for the current LEO user.</summary>
internal static class LeoOfficeScope
{
    internal sealed record LeoOfficeContext(User Leo, LocalOffice Office);

    public static async Task<Result<LeoOfficeContext>> ResolveAsync(
        IUserRepository users,
        ILocalOfficeRepository offices,
        Guid userId,
        CancellationToken ct)
    {
        var leo = await users.GetByIdAsync(userId, ct).ConfigureAwait(false);
        if (leo is null)
            return Errors.Users.UserNotFound;

        if (leo.Role != UserRole.LEO)
            return Errors.Auth.Forbidden;

        if (!leo.LocalOfficeId.HasValue)
            return Errors.Organization.OfficerNoOffice;

        var office = await offices.QueryAsNoTracking()
            .Include(o => o.Ward)
            .FirstOrDefaultAsync(o => o.Id == leo.LocalOfficeId.Value, ct)
            .ConfigureAwait(false);

        if (office is null)
            return Errors.Organization.OfficeNotFound;

        return new LeoOfficeContext(leo, office);
    }
}
