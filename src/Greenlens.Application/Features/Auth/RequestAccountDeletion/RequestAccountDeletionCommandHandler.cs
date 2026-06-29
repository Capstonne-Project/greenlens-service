using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.RequestAccountDeletion;

/// <summary>
/// User requests deletion of their own account.
/// Soft-deletes the user; after 90 days the AccountHardDeleteJob permanently removes data.
/// </summary>
/// <remarks>Implements: BR-AUTH-021 (soft delete 90 days, anonymize reports).</remarks>
public sealed class RequestAccountDeletionCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<RequestAccountDeletionCommandHandler> logger)
    : IRequestHandler<RequestAccountDeletionCommand, Result<RequestAccountDeletionResponse>>
{
    private const int RetentionDays = 90;

    public async Task<Result<RequestAccountDeletionResponse>> Handle(
        RequestAccountDeletionCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        if (user.IsDeleted)
            return Errors.Users.UserAlreadyDeleted;

        // Soft-delete user (sets DeletedAt, DeletedBy)
        user.SoftDelete(currentUser.Email);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var deletionDate = DateTime.UtcNow.AddDays(RetentionDays);

        logger.LogInformation(
            "User {UserId} requested account deletion. Will be hard-deleted after {Date}",
            user.Id, deletionDate);

        return new RequestAccountDeletionResponse(
            $"Tài khoản sẽ được xóa vĩnh viễn sau {RetentionDays} ngày. Bạn có thể khôi phục trước thời hạn.",
            deletionDate);
    }
}
