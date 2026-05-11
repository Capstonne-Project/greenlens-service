using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.VerifyPhoneFirebase;

/// <summary>
/// Verify phone number using Firebase Phone Auth.
/// FE sends the Firebase ID token after the user completes phone verification on the client.
/// BE verifies the token, extracts phone_number, and updates the user's profile.
/// </summary>
public sealed class VerifyPhoneFirebaseCommandHandler(
    IFirebasePhoneAuthService firebasePhoneAuth,
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser)
    : IRequestHandler<VerifyPhoneFirebaseCommand, Result<VerifyPhoneFirebaseResponse>>
{
    public async Task<Result<VerifyPhoneFirebaseResponse>> Handle(
        VerifyPhoneFirebaseCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verify the Firebase token and extract phone number
        var phoneInfo = await firebasePhoneAuth
            .VerifyPhoneTokenAsync(request.FirebaseIdToken, cancellationToken)
            .ConfigureAwait(false);

        if (phoneInfo is null)
            return Errors.Phone.FirebaseTokenInvalid;

        var phone = NormalizePhone(phoneInfo.PhoneNumber);

        if (string.IsNullOrWhiteSpace(phone))
            return Errors.Phone.FirebasePhoneMissing;

        // 2. Check if phone is already used by another user
        var phoneInUse = await users
            .ExistsAsync(u => u.PhoneNumber == phone && u.Id != currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (phoneInUse)
            return Errors.Phone.PhoneAlreadyUsed;

        // 3. Update the current user's phone
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        user.VerifyPhone(phone);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new VerifyPhoneFirebaseResponse(
            "Xác thực số điện thoại thành công.",
            true,
            phone);
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
