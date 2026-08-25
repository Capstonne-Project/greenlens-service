using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.ResetPassword;

/// <summary>Reset password using OTP code.</summary>
public sealed class ResetPasswordCommandHandler(
    IUserRepository users,
    IOtpRepository otps,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    ISystemSettingsProvider systemSettings,
    ILogger<ResetPasswordCommandHandler> logger)
    : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResponse>>
{
    public async Task<Result<ResetPasswordResponse>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var maxOtpAttempts = ModuleSystemSettings.OtpMaxAttempts(systemSettings);

        // Retrieve latest valid OTP for password reset
        var otp = await otps.GetLatestValidAsync(request.Email, OtpPurpose.PasswordReset, cancellationToken)
            .ConfigureAwait(false);

        if (otp is null || !otp.IsValid(maxOtpAttempts))
            return Errors.Auth.OtpExpired;

        // Track attempt count for rate limiting
        otp.IncrementAttempt();

        if (otp.HasExceededMaxAttempts(maxOtpAttempts))
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogWarning("OTP max attempts exceeded for {Email}", request.Email);
            return Errors.Auth.OtpMaxAttempts;
        }

        // Verify OTP code against stored hash
        if (!passwordHasher.Verify(request.OtpCode, otp.CodeHash))
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Errors.Auth.OtpInvalid;
        }

        // Find user by email
        var user = await users.GetByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        // Apply password reset and clear lockout
        otp.MarkUsed();
        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        user.ResetFailedLoginAttempts();

        // Revoke all refresh tokens on password reset (force re-login)
        await refreshTokens.RevokeAllByUserIdAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Password reset successfully for user {UserId}", user.Id);

        return new ResetPasswordResponse("Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.");
    }
}
