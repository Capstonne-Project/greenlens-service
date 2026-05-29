using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.ChangePassword;

/// <summary>Change password for authenticated user.</summary>
public sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    ILogger<ChangePasswordCommandHandler> logger)
    : IRequestHandler<ChangePasswordCommand, Result<ChangePasswordResponse>>
{
    public async Task<Result<ChangePasswordResponse>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        // Find authenticated user
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        // Verify current password before allowing change
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            logger.LogWarning("Incorrect current password for user {UserId}", currentUser.UserId);
            return Errors.Auth.IncorrectCurrentPassword;
        }

        // Apply new password hash
        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Password changed successfully for user {UserId}", currentUser.UserId);

        return new ChangePasswordResponse("Đổi mật khẩu thành công.");
    }
}
