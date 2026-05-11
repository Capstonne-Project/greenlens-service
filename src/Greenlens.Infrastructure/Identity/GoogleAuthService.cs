using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Identity;

/// <summary>Google Auth via Firebase Admin SDK. Placeholder — requires firebase-adminsdk.json.</summary>
internal sealed class GoogleAuthService(ILogger<GoogleAuthService> logger) : IGoogleAuthService
{
    public async Task<GoogleUserInfo?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        // TODO: Replace with actual Firebase Admin SDK verification
        // var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, ct);
        // return new GoogleUserInfo(decodedToken.Uid, decodedToken.Claims["email"], decodedToken.Claims["name"], decodedToken.Claims["picture"]);

        logger.LogWarning("GoogleAuthService is using placeholder implementation. Configure Firebase Admin SDK for production.");

        await Task.CompletedTask;
        return null;
    }
}
