using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.ChangePassword;

/// <summary>Change password for authenticated user.</summary>
public sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ChangePasswordCommand, Result<ChangePasswordResponse>>
{
    public async Task<Result<ChangePasswordResponse>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Errors.Auth.IncorrectCurrentPassword;

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ChangePasswordResponse("Đổi mật khẩu thành công.");
    }
}
