using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Auth.Login;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.GoogleLogin;

/// <summary>Login or register with Google via Firebase ID token.</summary>
public sealed class GoogleLoginCommandHandler(
    IGoogleAuthService googleAuth,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork uow,
    IJwtService jwtService,
    ILogger<GoogleLoginCommandHandler> logger)
    : IRequestHandler<GoogleLoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        GoogleLoginCommand request,
        CancellationToken cancellationToken)
    {
        // Verify Google ID token with Firebase
        var googleUser = await googleAuth.VerifyIdTokenAsync(request.IdToken, cancellationToken)
            .ConfigureAwait(false);

        if (googleUser is null)
        {
            logger.LogWarning("Google auth failed — invalid ID token");
            return Errors.Auth.GoogleAuthFailed;
        }

        // Try find existing user by Google ID or email
        var user = await users.GetByGoogleIdAsync(googleUser.GoogleId, cancellationToken)
            .ConfigureAwait(false);

        user ??= await users.GetByEmailAsync(googleUser.Email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            // Auto-register new user from Google profile
            user = User.CreateFromGoogle(
                googleUser.Email,
                googleUser.FullName,
                googleUser.GoogleId,
                googleUser.AvatarUrl);
            users.Add(user);
            logger.LogInformation("Auto-registered new user from Google {Email}", googleUser.Email);
        }
        else if (user.GoogleId is null)
        {
            // Link Google account to existing user
            user.LinkGoogleAccount(googleUser.GoogleId);
            if (!user.IsEmailVerified)
                user.VerifyEmail();
            logger.LogInformation("Linked Google account to existing user {UserId}", user.Id);
        }

        // Generate JWT access token and refresh token
        var accessToken = jwtService.GenerateAccessToken(user);
        var rawRefreshToken = jwtService.GenerateRefreshToken();
        var refreshTokenHash = jwtService.HashToken(rawRefreshToken);

        var refreshToken = Domain.Entities.RefreshToken.Create(user.Id, refreshTokenHash);
        refreshTokens.Add(refreshToken);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User {UserId} logged in via Google", user.Id);

        return new LoginResponse(
            accessToken,
            rawRefreshToken,
            new UserDto(user.Id, user.Email, user.FullName, user.Role.ToString(), user.IsEmailVerified, user.MustChangePassword));
    }
}
