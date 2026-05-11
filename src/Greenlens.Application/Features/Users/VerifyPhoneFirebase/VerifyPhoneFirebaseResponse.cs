namespace Greenlens.Application.Features.Users.VerifyPhoneFirebase;

/// <summary>Response after successful Firebase phone verification.</summary>
public sealed record VerifyPhoneFirebaseResponse(
    string Message,
    bool IsPhoneVerified,
    string PhoneNumber);
