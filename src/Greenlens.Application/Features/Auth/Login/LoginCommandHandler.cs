using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.Login;

/// <summary>Login with email and password.</summary>
/// <remarks>Implements: BR-AUTH-011 (lockout), BR-AUTH-013 (JWT + refresh).</remarks>
public sealed class LoginCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork uow,
    IJwtService jwtService,
    IPasswordHasher passwordHasher,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await users.GetByEmailAsync(
            request.Email.ToLowerInvariant(), cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.InvalidCredentials;

        // Check account lockout status
        if (user.IsLockedOut())
        {
            logger.LogWarning("Login attempt on locked account {Email}", request.Email);
            return Errors.Auth.AccountLocked;
        }

        // Verify email is confirmed
        if (!user.IsEmailVerified)
            return Errors.Auth.EmailNotVerified;

        // Verify password — record failed attempt on mismatch
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Errors.Auth.InvalidCredentials;
        }

        // Reset failed attempts on successful login
        user.ResetFailedLoginAttempts();

        // Generate JWT access token and refresh token
        var accessToken = jwtService.GenerateAccessToken(user);
        var rawRefreshToken = jwtService.GenerateRefreshToken();
        var refreshTokenHash = jwtService.HashToken(rawRefreshToken);

        var refreshToken = Domain.Entities.RefreshToken.Create(user.Id, refreshTokenHash);
        refreshTokens.Add(refreshToken);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User {UserId} logged in successfully", user.Id);

        return new LoginResponse(
            accessToken,
            rawRefreshToken,
            new UserDto(user.Id, user.Email, user.FullName, user.Role.ToString(), user.IsEmailVerified));
    }
}
