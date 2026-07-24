using System.Security.Cryptography;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.RequestOtp;

/// <summary>Generate and send OTP via email.</summary>
/// <remarks>OTP: 6 digits, 10 min lifetime.</remarks>
public sealed class RequestOtpCommandHandler(
    IUserRepository users,
    IOtpRepository otps,
    IUnitOfWork uow,
    IEmailSender emailSender,
    IPasswordHasher passwordHasher,
    ILogger<RequestOtpCommandHandler> logger)
    : IRequestHandler<RequestOtpCommand, Result<RequestOtpResponse>>
{
    public async Task<Result<RequestOtpResponse>> Handle(
        RequestOtpCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting OTP request");

        // Find user by email
        var user = await users.GetByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for email {Email}", request.Email);
            return Errors.Auth.UserNotFound;
        }

        // Invalidate previous OTPs for the same purpose
        await otps.InvalidateAllAsync(request.Email, request.Purpose, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Invalidating previous OTPs");

        // Generate 6-digit OTP and hash for storage
        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = passwordHasher.Hash(otpCode);

        var otp = OtpCode.Create(request.Email, codeHash, request.Purpose);
        otps.Add(otp);

        logger.LogInformation("OTP added: {Otp}", otp);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Send OTP via email
        await emailSender.SendOtpAsync(
            request.Email,
            otpCode,
            request.Purpose.ToString(),
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("OTP sent to {Email} for purpose {Purpose}", request.Email, request.Purpose);

        return new RequestOtpResponse("Mã OTP đã được gửi đến email của bạn.");
    }
}
