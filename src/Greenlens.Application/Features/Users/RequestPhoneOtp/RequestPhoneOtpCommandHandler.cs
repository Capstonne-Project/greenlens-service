using System.Security.Cryptography;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Users.RequestPhoneOtp;

/// <summary>
/// Generate OTP, send via SMS, and persist hashed OTP for later verification.
/// </summary>
/// <remarks>
/// Rate limited to 5 OTP requests per phone number per day.
/// </remarks>
public sealed class RequestPhoneOtpCommandHandler(
    IOtpRepository otps,
    IUnitOfWork uow,
    ISmsSender smsSender,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RequestPhoneOtpCommand, Result<RequestPhoneOtpResponse>>
{
    private const int MaxOtpPerDay = 5;

    public async Task<Result<RequestPhoneOtpResponse>> Handle(
        RequestPhoneOtpCommand request,
        CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.PhoneNumber);

        // Rate limit: max 5 OTP per phone per day
        var todayCount = await otps.CountTodayByPhoneAsync(phone, cancellationToken)
            .ConfigureAwait(false);

        if (todayCount >= MaxOtpPerDay)
            return Errors.Phone.OtpRateLimited;

        // Invalidate previous phone OTPs
        await otps.InvalidateAllByPhoneAsync(phone, cancellationToken)
            .ConfigureAwait(false);

        // Generate and hash OTP
        var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var codeHash = passwordHasher.Hash(otpCode);

        var otp = OtpCode.CreateForPhone(phone, codeHash);
        otps.Add(otp);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Send SMS (fire-and-forget style — OTP is persisted regardless)
        var sent = await smsSender.SendOtpAsync(phone, otpCode, cancellationToken)
            .ConfigureAwait(false);

        if (!sent)
            return Errors.Phone.SmsSendFailed;

        return new RequestPhoneOtpResponse("Mã OTP đã được gửi tới số điện thoại của bạn.");
    }

    /// <summary>Normalize VN phone to international format 84xxxxxxxxx.</summary>
    private static string NormalizePhone(string phone)
    {
        phone = phone.Trim().Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("+84"))
            return phone[1..]; // remove +
        if (phone.StartsWith("0"))
            return "84" + phone[1..];
        return phone;
    }
}
