using FirebaseAdmin.Auth;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Identity;

/// <summary>
/// Verifies Firebase Phone Auth ID tokens using the Firebase Admin SDK.
/// </summary>
internal sealed class FirebasePhoneAuthService(
    ILogger<FirebasePhoneAuthService> logger) : IFirebasePhoneAuthService
{
    public async Task<FirebasePhoneInfo?> VerifyPhoneTokenAsync(
        string idToken, CancellationToken ct = default)
    {
        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(idToken, ct)
                .ConfigureAwait(false);

            // Extract phone_number claim from Firebase token
            if (!decodedToken.Claims.TryGetValue("phone_number", out var phoneObj) ||
                phoneObj is not string phoneNumber ||
                string.IsNullOrWhiteSpace(phoneNumber))
            {
                logger.LogWarning(
                    "Firebase token for UID {Uid} does not contain phone_number claim",
                    decodedToken.Uid);
                return null;
            }

            logger.LogInformation(
                "Firebase phone verified for UID {Uid}: {Phone}",
                decodedToken.Uid, MaskPhone(phoneNumber));

            return new FirebasePhoneInfo(decodedToken.Uid, phoneNumber);
        }
        catch (FirebaseAuthException ex)
        {
            logger.LogWarning(ex, "Firebase token verification failed: {Message}", ex.Message);
            return null;
        }
    }

    private static string MaskPhone(string phone) =>
        phone.Length > 4 ? phone[..^4] + "****" : "****";
}
