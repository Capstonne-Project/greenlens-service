using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Auth.Login;

/// <summary>Login with email and password.</summary>
/// <remarks>Implements: BR-AUTH-011 (lockout), BR-AUTH-013 (JWT + refresh).</remarks>
public sealed class LoginCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork uow,
    IJwtService jwtService,
    IPasswordHasher passwordHasher)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(
            request.Email.ToLowerInvariant(), cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.InvalidCredentials;

        if (user.IsLockedOut())
            return Errors.Auth.AccountLocked;

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Errors.Auth.InvalidCredentials;
        }

        user.ResetFailedLoginAttempts();

        var accessToken = jwtService.GenerateAccessToken(user);
        var rawRefreshToken = jwtService.GenerateRefreshToken();
        var refreshTokenHash = jwtService.HashToken(rawRefreshToken);

        var refreshToken = Domain.Entities.RefreshToken.Create(user.Id, refreshTokenHash);
        refreshTokens.Add(refreshToken);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new LoginResponse(
            accessToken,
            rawRefreshToken,
            new UserDto(user.Id, user.Email, user.FullName, user.Role.ToString(), user.IsEmailVerified));
    }
}
