using System.Security.Cryptography;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Auth.ForgotPassword;

/// <summary>Send password reset OTP to email.</summary>
public sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IOtpRepository otps,
    IUnitOfWork uow,
    IEmailSender emailSender,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    public async Task<Result<ForgotPasswordResponse>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        // Always return success to prevent email enumeration
        if (user is null)
            return new ForgotPasswordResponse("Nếu email tồn tại, mã OTP sẽ được gửi.");

        await otps.InvalidateAllAsync(request.Email, OtpPurpose.PasswordReset, cancellationToken)
            .ConfigureAwait(false);

        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = passwordHasher.Hash(otpCode);

        var otp = OtpCode.Create(request.Email, codeHash, OtpPurpose.PasswordReset);
        otps.Add(otp);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await emailSender.SendPasswordResetAsync(request.Email, otpCode, cancellationToken)
            .ConfigureAwait(false);

        return new ForgotPasswordResponse("Nếu email tồn tại, mã OTP sẽ được gửi.");
    }
}
