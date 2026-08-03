using System.Security.Cryptography;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.RequestOtp;

/// <summary>Generate and enqueue OTP email delivery.</summary>
/// <remarks>
/// OTP: 6 digits, 10 min lifetime. BR-SYS-001: email sent via Hangfire background job.
/// </remarks>
public sealed class RequestOtpCommandHandler(
    IUserRepository users,
    IOtpRepository otps,
    IUnitOfWork uow,
    IAuthEmailScheduler authEmailScheduler,
    IPasswordHasher passwordHasher,
    ILogger<RequestOtpCommandHandler> logger)
    : IRequestHandler<RequestOtpCommand, Result<RequestOtpResponse>>
{
    public async Task<Result<RequestOtpResponse>> Handle(
        RequestOtpCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting OTP request");

        var user = await users.GetByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for email {Email}", request.Email);
            return Errors.Auth.UserNotFound;
        }

        await otps.InvalidateAllAsync(request.Email, request.Purpose, cancellationToken)
            .ConfigureAwait(false);

        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = passwordHasher.Hash(otpCode);

        var otp = OtpCode.Create(request.Email, codeHash, request.Purpose);
        otps.Add(otp);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!authEmailScheduler.TryEnqueueOtpEmail(
                request.Email,
                otpCode,
                request.Purpose.ToString()))
        {
            logger.LogError("OTP persisted but email job enqueue failed for user {UserId}", user.Id);
            return Errors.Auth.EmailDispatchUnavailable;
        }

        logger.LogInformation("OTP enqueued for user {UserId}, purpose {Purpose}", user.Id, request.Purpose);

        return new RequestOtpResponse("Mã OTP đã được gửi đến email của bạn.");
    }
}
