using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Users.GetProfile;

/// <summary>
/// Get the authenticated user's own profile.
/// </summary>
public sealed class GetProfileQueryHandler(
    IUserRepository users,
    ICurrentUser currentUser)
    : IRequestHandler<GetProfileQuery, Result<UserDetailDto>>
{
    public async Task<Result<UserDetailDto>> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await users.QueryAsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new UserDetailDto(
                u.Id,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.AvatarUrl,
                u.Role,
                u.IsEmailVerified,
                u.GoogleId,
                u.CreatedAt,
                u.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dto is null
            ? Result<UserDetailDto>.Failure(Errors.Users.UserNotFound)
            : dto;
    }
}
