using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.UpdateUser;

/// <summary>
/// Admin updates a user's details (name, phone, role, verification status).
/// </summary>
/// <remarks>
/// Implements: BR-ADM (admin user management).
/// </remarks>
public sealed class UpdateUserCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ILogger<UpdateUserCommandHandler> logger)
    : IRequestHandler<UpdateUserCommand, Result<UpdateUserResponse>>
{
    public async Task<Result<UpdateUserResponse>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Users.UserNotFound;

        // Admin updates user details
        user.AdminUpdate(
            request.FullName,
            request.PhoneNumber,
            request.Role,
            request.IsEmailVerified);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Admin updated user {UserId}", request.UserId);

        return new UpdateUserResponse(user.Id, "Cập nhật người dùng thành công.");
    }
}
