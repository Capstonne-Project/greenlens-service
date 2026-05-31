using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Users.GetAllUsers;

/// <summary>
/// Fetch all users (no pagination). Admin only.
/// </summary>
public sealed class GetAllUsersQueryHandler(IUserRepository users,
    ILogger<GetAllUsersQueryHandler> logger)
    : IRequestHandler<GetAllUsersQuery, Result<List<UserListItemDto>>>
{
    public async Task<Result<List<UserListItemDto>>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        var list = await users.QueryAsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.AvatarUrl,
                u.Role,
                u.IsEmailVerified,
                u.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Lấy danh sách người dùng thành công. Số lượng: {Count}", list.Count);
        return list;
    }
}
