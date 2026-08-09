using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Identity;

/// <summary>
/// Verifies Google Sign-In ID tokens issued by Firebase Authentication.
/// </summary>
/// <remarks>
/// Requires Firebase Admin SDK initialization (see <c>Firebase:ServiceAccountKeyPath</c>).
/// The mobile client signs in with Google through Firebase and posts the resulting
/// Firebase ID token to <c>POST /v1/auth/google-login</c>.
/// </remarks>
internal sealed class GoogleAuthService(ILogger<GoogleAuthService> logger) : IGoogleAuthService
{
    public async Task<GoogleUserInfo?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            logger.LogWarning("Google login attempted with an empty ID token");
            return null;
        }

        // Without a service account key the SDK never initializes; fail loudly here so the
        // cause is obvious instead of surfacing as a generic invalid-token error.
        if (FirebaseApp.DefaultInstance is null)
        {
            logger.LogError(
                "Firebase Admin SDK is not initialized — set Firebase:ServiceAccountKeyPath. " +
                "Google login cannot verify ID tokens.");
            return null;
        }

        try
        {
            var decodedToken = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(idToken, ct)
                .ConfigureAwait(false);

            if (!TryGetClaim(decodedToken, "email", out var email))
            {
                logger.LogWarning(
                    "Firebase token for UID {Uid} does not contain an email claim",
                    decodedToken.Uid);
                return null;
            }

            // Google accounts always carry a verified email; a false claim means the address
            // was never confirmed and must not be trusted to match an existing user by email.
            if (decodedToken.Claims.TryGetValue("email_verified", out var verifiedObj)
                && verifiedObj is bool and false)
            {
                logger.LogWarning("Firebase token for UID {Uid} has an unverified email", decodedToken.Uid);
                return null;
            }

            TryGetClaim(decodedToken, "name", out var fullName);
            TryGetClaim(decodedToken, "picture", out var avatarUrl);

            logger.LogInformation("Google ID token verified for UID {Uid}", decodedToken.Uid);

            return new GoogleUserInfo(
                decodedToken.Uid,
                email,
                string.IsNullOrWhiteSpace(fullName) ? email : fullName,
                string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl);
        }
        catch (FirebaseAuthException ex)
        {
            logger.LogWarning(ex, "Google ID token verification failed: {Message}", ex.Message);
            return null;
        }
    }

    private static bool TryGetClaim(FirebaseToken token, string claim, out string value)
    {
        if (token.Claims.TryGetValue(claim, out var raw)
            && raw is string str
            && !string.IsNullOrWhiteSpace(str))
        {
            value = str;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
