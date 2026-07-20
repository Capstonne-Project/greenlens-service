using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.DeleteUser;

/// <summary>
/// Soft-delete a user (sets IsDeleted / DeletedAt). Admin only.
/// </summary>
/// <remarks>
/// Implements: BR-AUTH-022 (soft delete).
/// </remarks>
public sealed class DeleteUserCommandHandler(
    IUserRepository users,
    IUserPointsRepository userPointsRepo,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeleteUserCommandHandler> logger)
    : IRequestHandler<DeleteUserCommand, Result<DeleteUserResponse>>
{
    public async Task<Result<DeleteUserResponse>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == currentUser.UserId)
            return Errors.Users.CannotDeleteSelf;

        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Users.UserNotFound;

        if (user.IsDeleted)
            return Errors.Users.UserAlreadyDeleted;

        // Soft-delete the user (sets IsDeleted / DeletedAt)
        user.SoftDelete(currentUser.Email);

        // Also soft-delete gamification points if exists
        var userPoints = await userPointsRepo.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        userPoints?.SoftDelete(currentUser.Email);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogWarning("User {TargetUserId} soft-deleted by admin {AdminId}",
            request.UserId, currentUser.UserId);

        return new DeleteUserResponse(user.Id, "Xóa người dùng thành công.");
    }
}
