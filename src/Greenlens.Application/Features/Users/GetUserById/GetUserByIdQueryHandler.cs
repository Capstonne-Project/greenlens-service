using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Users.GetUserById;

/// <summary>
/// Fetch a single user by ID. Admin only.
/// </summary>
public sealed class GetUserByIdQueryHandler(IUserRepository users)
    : IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>
{
    public async Task<Result<UserDetailDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await users.QueryAsNoTracking()
            .Where(u => u.Id == request.UserId)
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
