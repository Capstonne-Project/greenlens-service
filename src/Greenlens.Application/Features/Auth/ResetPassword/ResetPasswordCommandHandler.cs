using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Auth.ResetPassword;

/// <summary>Reset password using OTP code.</summary>
public sealed class ResetPasswordCommandHandler(
    IUserRepository users,
    IOtpRepository otps,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResponse>>
{
    public async Task<Result<ResetPasswordResponse>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var otp = await otps.GetLatestValidAsync(request.Email, OtpPurpose.PasswordReset, cancellationToken)
            .ConfigureAwait(false);

        if (otp is null || !otp.IsValid)
            return Errors.Auth.OtpExpired;

        otp.IncrementAttempt();

        if (!passwordHasher.Verify(request.OtpCode, otp.CodeHash))
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Errors.Auth.OtpInvalid;
        }

        var user = await users.GetByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        otp.MarkUsed();
        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        user.ResetFailedLoginAttempts();

        // Revoke all refresh tokens on password reset
        await refreshTokens.RevokeAllByUserIdAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResetPasswordResponse("Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.");
    }
}
