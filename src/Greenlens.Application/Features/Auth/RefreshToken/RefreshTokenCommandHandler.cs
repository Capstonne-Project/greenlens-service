using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Auth.Login;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.RefreshToken;

/// <summary>Refresh access token using refresh token rotation.</summary>
/// <remarks>Implements: BR-AUTH-013 (refresh 30d, rotation).</remarks>
public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IUnitOfWork uow,
    IJwtService jwtService,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // Hash incoming token and look up in DB
        var tokenHash = jwtService.HashToken(request.RefreshToken);

        var existingToken = await refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (existingToken is null || !existingToken.IsActive)
        {
            logger.LogWarning("Invalid or expired refresh token used");
            return Errors.Auth.InvalidRefreshToken;
        }

        // Find the token owner
        var user = await users.GetByIdAsync(existingToken.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        // Rotate: revoke old token, create new one
        var newRawToken = jwtService.GenerateRefreshToken();
        var newTokenHash = jwtService.HashToken(newRawToken);

        existingToken.Revoke(newTokenHash);

        var newRefreshToken = Domain.Entities.RefreshToken.Create(user.Id, newTokenHash);
        refreshTokens.Add(newRefreshToken);

        // Generate new access token
        var accessToken = jwtService.GenerateAccessToken(user);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Token refreshed for user {UserId}", user.Id);

        return new LoginResponse(
            accessToken,
            newRawToken,
            new UserDto(user.Id, user.Email, user.FullName, user.Role.ToString(), user.IsEmailVerified));
    }
}
