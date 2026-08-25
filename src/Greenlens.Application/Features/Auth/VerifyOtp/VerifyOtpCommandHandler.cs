using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.VerifyOtp;

/// <summary>Verify OTP code for email verification or password reset.</summary>
public sealed class VerifyOtpCommandHandler(
    IOtpRepository otps,
    IUserRepository users,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    ISystemSettingsProvider systemSettings,
    ILogger<VerifyOtpCommandHandler> logger)
    : IRequestHandler<VerifyOtpCommand, Result<VerifyOtpResponse>>
{
    public async Task<Result<VerifyOtpResponse>> Handle(
        VerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting OTP verification");

        var maxOtpAttempts = ModuleSystemSettings.OtpMaxAttempts(systemSettings);

        // Retrieve latest valid OTP for the given purpose
        var otp = await otps.GetLatestValidAsync(request.Email, request.Purpose, cancellationToken)
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
            logger.LogWarning("OTP invalid for {Email}", request.Email);
            return Errors.Auth.OtpInvalid;
        }

        // Mark OTP as used
        otp.MarkUsed();

        // If email verification purpose, mark user email as verified
        if (request.Purpose == OtpPurpose.EmailVerification)
        {
            var user = await users.GetByEmailAsync(request.Email, cancellationToken)
                .ConfigureAwait(false);
            logger.LogInformation("User: {User}", user);
            if (user is not null)
            {
                user.VerifyEmail();
                logger.LogInformation("Email verified for {Email}", request.Email);
            }
        }

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("OTP verified for {Email} purpose {Purpose}", request.Email, request.Purpose);

        return new VerifyOtpResponse("Xác thực OTP thành công.", true);
    }
}
