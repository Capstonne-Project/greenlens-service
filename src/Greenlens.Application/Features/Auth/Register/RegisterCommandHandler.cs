using System.Security.Cryptography;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.Register;

/// <summary>
/// Register a new citizen account and send email verification OTP.
/// </summary>
/// <remarks>
/// Implements: BR-AUTH-005 (password strength), BR-AUTH-021 (deleted email → restore hint),
/// BR-DAT-001 (bcrypt ≥12).
/// </remarks>
public sealed class RegisterCommandHandler(
    IUserRepository users,
    IOtpRepository otps,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ILogger<RegisterCommandHandler> logger)
    : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var emailError = await UserRegistrationGuard
            .ValidateNewEmailForRegistrationAsync(users, request.Email, cancellationToken)
            .ConfigureAwait(false);
        if (emailError is not null)
            return emailError;

        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, passwordHash, request.FullName);

        users.Add(user);

        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = passwordHasher.Hash(otpCode);
        var otp = OtpCode.Create(user.Email, codeHash, OtpPurpose.EmailVerification);
        otps.Add(otp);

        try
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            var mapped = PostgresUniqueViolationMapper.TryMap(ex);
            if (mapped is not null)
                return mapped;
            throw;
        }

        await emailSender.SendOtpAsync(
            user.Email,
            otpCode,
            OtpPurpose.EmailVerification.ToString(),
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("New user registered {UserId}", user.Id);

        return new RegisterResponse(
            user.Id,
            user.Email,
            "Đăng ký thành công. Mã OTP đã được gửi đến email của bạn.");
    }
}
