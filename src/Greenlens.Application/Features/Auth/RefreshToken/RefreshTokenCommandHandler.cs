using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Auth.Login;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.RefreshToken;

/// <summary>Refresh access token using refresh token rotation.</summary>
/// <remarks>Implements: BR-AUTH-013 (refresh 30d, rotation).</remarks>
public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IUnitOfWork uow,
    IJwtService jwtService)
    : IRequestHandler<RefreshTokenCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = jwtService.HashToken(request.RefreshToken);

        var existingToken = await refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (existingToken is null || !existingToken.IsActive)
            return Errors.Auth.InvalidRefreshToken;

        var user = await users.GetByIdAsync(existingToken.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        // Rotate: revoke old, create new
        var newRawToken = jwtService.GenerateRefreshToken();
        var newTokenHash = jwtService.HashToken(newRawToken);

        existingToken.Revoke(newTokenHash);

        var newRefreshToken = Domain.Entities.RefreshToken.Create(user.Id, newTokenHash);
        refreshTokens.Add(newRefreshToken);

        var accessToken = jwtService.GenerateAccessToken(user);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new LoginResponse(
            accessToken,
            newRawToken,
            new UserDto(user.Id, user.Email, user.FullName, user.Role.ToString(), user.IsEmailVerified));
    }
}
