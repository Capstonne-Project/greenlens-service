using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.ToggleBanUser;

/// <summary>
/// Toggle ban/unban status for a user. Admin only.
/// </summary>
/// <remarks>Implements: BR-AUTH-015 (block banned users from login).</remarks>
public sealed class ToggleBanUserCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<ToggleBanUserCommandHandler> logger)
    : IRequestHandler<ToggleBanUserCommand, Result<ToggleBanUserResponse>>
{
    public async Task<Result<ToggleBanUserResponse>> Handle(
        ToggleBanUserCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Toggling ban user");

        if (request.UserId == currentUser.UserId)
        {
            logger.LogWarning("Cannot ban yourself: {UserId}", request.UserId);
            return Errors.Users.CannotDeleteSelf; // can't ban yourself
        }

        logger.LogInformation("Getting user: {UserId}", request.UserId);

        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found: {UserId}", request.UserId);
            return Errors.Users.UserNotFound;
        }

        user.ToggleBan();
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("User {UserId} banned successfully", request.UserId);

        var action = user.IsBanned ? "cấm" : "bỏ cấm";
        logger.LogWarning("User {TargetUserId} {Action} by admin {AdminId}",
            request.UserId, action, currentUser.UserId);

        return new ToggleBanUserResponse(
            user.Id,
            user.IsBanned,
            user.IsBanned
                ? "Đã cấm tài khoản thành công."
                : "Đã bỏ cấm tài khoản thành công.");
    }
}
