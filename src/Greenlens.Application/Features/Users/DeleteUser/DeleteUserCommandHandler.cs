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
/// Implements: BR-AUTH-021 (anonymize reports), BR-AUTH-022 (soft delete).
/// </remarks>
public sealed class DeleteUserCommandHandler(
    IUserRepository users,
    IUserPointsRepository userPointsRepo,
    IReportRepository reports,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeleteUserCommandHandler> logger)
    : IRequestHandler<DeleteUserCommand, Result<DeleteUserResponse>>
{
    public async Task<Result<DeleteUserResponse>> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting user {UserId} for admin {AdminId}", request.UserId, currentUser.UserId);

        if (request.UserId == currentUser.UserId)
        {
            logger.LogWarning("Cannot delete self for user {UserId}", request.UserId);
            return Errors.Users.CannotDeleteSelf;
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for ID {UserId}", request.UserId);
            return Errors.Users.UserNotFound;
        }

        if (user.IsDeleted)
        {
            logger.LogWarning("User {UserId} already deleted", request.UserId);
            return Errors.Users.UserAlreadyDeleted;
        }

        user.SoftDelete(currentUser.Email);

        var userPoints = await userPointsRepo.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (userPoints is not null)
        {
            logger.LogInformation("Soft-deleting user points for user {UserId}", user.Id);
            userPoints.SoftDelete(currentUser.Email);
        }

        var anonymizedCount = await reports
            .AnonymizeReporterAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "User {TargetUserId} soft-deleted by admin {AdminId}; {ReportCount} reports anonymized",
            request.UserId, currentUser.UserId, anonymizedCount);

        return new DeleteUserResponse(user.Id, "Xóa người dùng thành công.");
    }
}
