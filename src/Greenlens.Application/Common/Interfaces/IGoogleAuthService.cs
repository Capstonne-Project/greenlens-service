namespace Greenlens.Application.Common.Interfaces;

public sealed record GoogleUserInfo(string GoogleId, string Email, string FullName, string? AvatarUrl);

public interface IGoogleAuthService
{
    Task<GoogleUserInfo?> VerifyIdTokenAsync(string idToken, CancellationToken ct = default);
}
