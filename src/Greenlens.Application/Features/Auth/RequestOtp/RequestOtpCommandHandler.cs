using System.Security.Cryptography;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Auth.RequestOtp;

/// <summary>Generate and send OTP via email.</summary>
/// <remarks>OTP: 6 digits, 10 min lifetime.</remarks>
public sealed class RequestOtpCommandHandler(
    IUserRepository users,
    IOtpRepository otps,
    IUnitOfWork uow,
    IEmailSender emailSender,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RequestOtpCommand, Result<RequestOtpResponse>>
{
    public async Task<Result<RequestOtpResponse>> Handle(
        RequestOtpCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        // Invalidate previous OTPs
        await otps.InvalidateAllAsync(request.Email, request.Purpose, cancellationToken)
            .ConfigureAwait(false);

        // Generate 6-digit OTP
        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = passwordHasher.Hash(otpCode);

        var otp = OtpCode.Create(request.Email, codeHash, request.Purpose);
        otps.Add(otp);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Send email
        await emailSender.SendOtpAsync(
            request.Email,
            otpCode,
            request.Purpose.ToString(),
            cancellationToken).ConfigureAwait(false);

        return new RequestOtpResponse("Mã OTP đã được gửi đến email của bạn.");
    }
}
