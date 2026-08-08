using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.LookupCitizenByEmail;

/// <summary>
/// Looks up a user by exact email and returns their info + recruit eligibility.
/// Does NOT modify any data — read-only preview for LEO dashboard.
/// </summary>
public sealed class LookupCitizenByEmailQueryHandler(
    ICurrentUser currentUser,
    IUserRepository users,
    ILocalOfficeRepository offices,
    ILogger<LookupCitizenByEmailQueryHandler> logger) : IRequestHandler<LookupCitizenByEmailQuery, Result<CitizenLookupResponse>>
{
    public async Task<Result<CitizenLookupResponse>> Handle(
        LookupCitizenByEmailQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Looking up citizen by email {Email}", request.Email);

        var user = await users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for email {Email}", request.Email);
            return Errors.Users.UserNotFound;
        }

        // Resolve LEO's office to compare wards
        var leoOffice = await offices.QueryAsNoTracking()
            .FirstOrDefaultAsync(o => o.OfficerId == currentUser.UserId, ct)
            .ConfigureAwait(false);

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
            var isInMyWard = leoOffice is not null && user.LocalOfficeId == leoOffice.Id;

            isEligible = false;
            reason = isInMyWard
                ? null // Citizen đã ở phường mình — không hiện message, vẫn tìm thấy để add vào team
                : "Người dùng đã thuộc một phường/xã khác.";
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

