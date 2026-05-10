using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.DeleteUser;

/// <summary>
/// Soft-delete a user (sets IsDeleted / DeletedAt). Admin only.
/// </summary>
/// <remarks>
/// Implements: BR-AUTH-022 (soft delete).
/// </remarks>
public sealed class DeleteUserCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser)
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

        user.SoftDelete(currentUser.Email);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeleteUserResponse(user.Id, "Xóa người dùng thành công.");
    }
}
