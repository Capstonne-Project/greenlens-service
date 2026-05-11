using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Users.VerifyPhoneOtp;

/// <summary>
/// Verify phone OTP, then update the user's phone number and mark as verified.
/// </summary>
public sealed class VerifyPhoneOtpCommandHandler(
    IOtpRepository otps,
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher)
    : IRequestHandler<VerifyPhoneOtpCommand, Result<VerifyPhoneOtpResponse>>
{
    public async Task<Result<VerifyPhoneOtpResponse>> Handle(
        VerifyPhoneOtpCommand request,
        CancellationToken cancellationToken)
    {
        var phone = NormalizePhone(request.PhoneNumber);

        // 1. Get latest valid OTP for this phone
        var otp = await otps.GetLatestValidByPhoneAsync(phone, cancellationToken)
            .ConfigureAwait(false);

        if (otp is null || !otp.IsValid)
            return Errors.Auth.OtpExpired;

        otp.IncrementAttempt();

        // 2. Check max attempts
        if (otp.HasExceededMaxAttempts)
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Errors.Auth.OtpMaxAttempts;
        }

        // 3. Verify OTP code
        if (!passwordHasher.Verify(request.OtpCode, otp.CodeHash))
        {
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Errors.Auth.OtpInvalid;
        }

        // 4. Check phone not already used by another user
        var phoneInUse = await users.QueryAsNoTracking()
            .AnyAsync(u => u.PhoneNumber == phone && u.Id != currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (phoneInUse)
            return Errors.Phone.PhoneAlreadyUsed;

        // 5. Update user's phone
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Users.UserNotFound;

        otp.MarkUsed();
        user.VerifyPhone(phone);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new VerifyPhoneOtpResponse("Xác thực số điện thoại thành công.", true);
    }

    private static string NormalizePhone(string phone)
    {
        phone = phone.Trim().Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("+84"))
            return phone[1..];
        if (phone.StartsWith("0"))
            return "84" + phone[1..];
        return phone;
    }
}
