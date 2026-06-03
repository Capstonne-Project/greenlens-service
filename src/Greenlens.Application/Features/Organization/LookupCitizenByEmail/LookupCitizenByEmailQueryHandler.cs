using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.LookupCitizenByEmail;

/// <summary>
/// Looks up a user by exact email and returns their info + recruit eligibility.
/// Does NOT modify any data — read-only preview for LEO dashboard.
/// </summary>
public sealed class LookupCitizenByEmailQueryHandler(
    IUserRepository users) : IRequestHandler<LookupCitizenByEmailQuery, Result<CitizenLookupResponse>>
{
    public async Task<Result<CitizenLookupResponse>> Handle(
        LookupCitizenByEmailQuery request,
        CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Users.UserNotFound;

        // Determine eligibility
        var isEligible = true;
        string? reason = null;

        if (user.Role != UserRole.Citizen)
        {
            isEligible = false;
            reason = $"Người dùng đã có vai trò {user.Role}. Chỉ Citizen mới được recruit.";
        }
        else if (user.LocalOfficeId.HasValue)
        {
            isEligible = false;
            reason = "Người dùng đã thuộc một phường/xã khác.";
        }

        return new CitizenLookupResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.AvatarUrl,
            user.Role,
            isEligible,
            reason);
    }
}
