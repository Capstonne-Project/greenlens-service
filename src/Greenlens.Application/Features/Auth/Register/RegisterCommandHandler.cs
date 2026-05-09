using System.Security.Cryptography;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Auth.Register;

/// <summary>
/// Register a new citizen account and send email verification OTP.
/// </summary>
/// <remarks>
/// Implements: BR-AUTH-005 (password strength), BR-DAT-001 (bcrypt ≥12).
/// </remarks>
public sealed class RegisterCommandHandler(
    IUserRepository users,
    IOtpRepository otps,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender)
    : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var emailExists = await users.ExistsAsync(
            u => u.Email == request.Email.ToLowerInvariant(),
            cancellationToken).ConfigureAwait(false);

        if (emailExists)
            return Errors.Auth.EmailTaken;

        var passwordHash = passwordHasher.Hash(request.Password);

        var user = User.Create(
            request.Email,
            passwordHash,
            request.FullName);

        users.Add(user);

        // ── Generate & send OTP for email verification ──
        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = passwordHasher.Hash(otpCode);

        var otp = OtpCode.Create(user.Email, codeHash, OtpPurpose.EmailVerification);
        otps.Add(otp);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Send OTP email (fire after DB commit)
        await emailSender.SendOtpAsync(
            user.Email,
            otpCode,
            OtpPurpose.EmailVerification.ToString(),
            cancellationToken).ConfigureAwait(false);

        return new RegisterResponse(
            user.Id,
            user.Email,
            "Đăng ký thành công. Mã OTP đã được gửi đến email của bạn.");
    }
}
