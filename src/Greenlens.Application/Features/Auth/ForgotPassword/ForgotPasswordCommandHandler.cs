using System.Security.Cryptography;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.ForgotPassword;

/// <summary>Send password reset OTP to email.</summary>
public sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IOtpRepository otps,
    IUnitOfWork uow,
    IEmailSender emailSender,
    IPasswordHasher passwordHasher,
    ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    public async Task<Result<ForgotPasswordResponse>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await users.GetByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);
        logger.LogInformation("User: {User}", user);
        // Always return success to prevent email enumeration
        if (user is null)
        {
            logger.LogInformation("Forgot password requested for non-existent email {Email}", request.Email);
            return new ForgotPasswordResponse("Nếu email tồn tại, mã OTP sẽ được gửi.");
        }

        // Invalidate all previous password reset OTPs
        await otps.InvalidateAllAsync(request.Email, OtpPurpose.PasswordReset, cancellationToken)
            .ConfigureAwait(false);

        // Generate 6-digit OTP and hash for storage
        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = passwordHasher.Hash(otpCode);

        var otp = OtpCode.Create(request.Email, codeHash, OtpPurpose.PasswordReset);
        otps.Add(otp);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Send password reset email
        await emailSender.SendPasswordResetAsync(request.Email, otpCode, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Password reset OTP sent to {Email}", request.Email);

        return new ForgotPasswordResponse("Nếu email tồn tại, mã OTP sẽ được gửi.");
    }
}
