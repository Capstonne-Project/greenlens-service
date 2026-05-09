using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Auth.VerifyOtp;

/// <summary>Verify OTP code for email verification or password reset.</summary>
public sealed class VerifyOtpCommandHandler(
    IOtpRepository otps,
    IUserRepository users,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher)
    : IRequestHandler<VerifyOtpCommand, Result<VerifyOtpResponse>>
{
    public async Task<Result<VerifyOtpResponse>> Handle(
        VerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        var otp = await otps.GetLatestValidAsync(request.Email, request.Purpose, cancellationToken)
            .ConfigureAwait(false);

        if (otp is null || !otp.IsValid)
            return Errors.Auth.OtpExpired;

        otp.IncrementAttempt();

        if (otp.HasExceededMaxAttempts)
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Errors.Auth.OtpMaxAttempts;
        }

        if (!passwordHasher.Verify(request.OtpCode, otp.CodeHash))
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Errors.Auth.OtpInvalid;
        }

        otp.MarkUsed();

        // If email verification, mark user as verified
        if (request.Purpose == OtpPurpose.EmailVerification)
        {
            var user = await users.GetByEmailAsync(request.Email, cancellationToken)
                .ConfigureAwait(false);
            user?.VerifyEmail();
        }

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new VerifyOtpResponse("Xác thực OTP thành công.", true);
    }
}
