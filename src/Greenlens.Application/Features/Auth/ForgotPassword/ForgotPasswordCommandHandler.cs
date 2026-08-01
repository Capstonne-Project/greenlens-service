using System.Security.Cryptography;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.ForgotPassword;

/// <summary>Enqueue password reset OTP email (anti-enumeration — always success when user exists).</summary>
/// <remarks>
/// BR-SYS-001: SMTP via Hangfire. Always returns generic success message (anti-enumeration),
/// even when enqueue or background delivery fails.
/// </remarks>
public sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IOtpRepository otps,
    IUnitOfWork uow,
    IAuthEmailScheduler authEmailScheduler,
    IPasswordHasher passwordHasher,
    ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    public async Task<Result<ForgotPasswordResponse>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogInformation("Forgot password requested for non-existent email {Email}", request.Email);
            return new ForgotPasswordResponse("Nếu email tồn tại, mã OTP sẽ được gửi.");
        }

        await otps.InvalidateAllAsync(request.Email, OtpPurpose.PasswordReset, cancellationToken)
            .ConfigureAwait(false);

        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = passwordHasher.Hash(otpCode);

        var otp = OtpCode.Create(request.Email, codeHash, OtpPurpose.PasswordReset);
        otps.Add(otp);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!authEmailScheduler.TryEnqueuePasswordResetEmail(request.Email, otpCode))
        {
            logger.LogError(
                "Password reset OTP persisted but email job enqueue failed for user {UserId}",
                user.Id);
        }

        logger.LogInformation("Password reset OTP enqueued for user {UserId}", user.Id);

        return new ForgotPasswordResponse("Nếu email tồn tại, mã OTP sẽ được gửi.");
    }
}
