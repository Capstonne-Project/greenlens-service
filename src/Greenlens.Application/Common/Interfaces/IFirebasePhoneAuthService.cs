namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Verifies Firebase Phone Auth ID tokens on the backend.
/// </summary>
public interface IFirebasePhoneAuthService
{
    /// <summary>
    /// Verify a Firebase ID token and extract the phone number.
    /// </summary>
    /// <returns>Phone info on success, null if token is invalid.</returns>
    Task<FirebasePhoneInfo?> VerifyPhoneTokenAsync(string idToken, CancellationToken ct = default);
}

/// <summary>Firebase-verified phone information.</summary>
public sealed record FirebasePhoneInfo(string Uid, string PhoneNumber);
