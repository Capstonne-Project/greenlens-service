using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.Login;

/// <summary>Login with email and password.</summary>
/// <remarks>
/// Implements: BR-AUTH-013 (login), BR-AUTH-011 (lockout), BR-AUTH-015 (block banned/expired),
/// BR-AUTH-016 (JWT + refresh).
/// </remarks>
public sealed class LoginCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    ICompanyStaffRepository companyStaff,
    IUnitOfWork uow,
    IJwtService jwtService,
    IPasswordHasher passwordHasher,
    ISystemSettingsProvider systemSettings,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var (maxFailedAttempts, lockoutMinutes, captchaAfterAttempts) =
            ModuleSystemSettings.AuthLockout(systemSettings);

        var user = await users.GetByEmailAsync(
            request.Email.ToLowerInvariant(), cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.InvalidCredentials;

        if (user.IsBanned)
        {
            logger.LogWarning("Login attempt on banned account {UserId}", user.Id);
            return Errors.Auth.AccountBanned;
        }

        if (user.IsDeleted)
            return Errors.Auth.AccountDeactivated;

        if (user.IsLockedOut())
        {
            logger.LogWarning("Login attempt on locked account {UserId}", user.Id);
            return Errors.Auth.AccountLocked;
        }

        if (!user.IsEmailVerified)
            return Errors.Auth.EmailNotVerified;

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(maxFailedAttempts, lockoutMinutes);
            if (user.RequiresCaptcha(captchaAfterAttempts))
                logger.LogDebug("User {UserId} requires captcha on next login attempt", user.Id);
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogWarning("Failed login attempt for user {UserId}", user.Id);
            return Errors.Auth.InvalidCredentials;
        }

        if (user.Role is UserRole.CompanyManager or UserRole.CompanyStaff)
        {
            var staff = await companyStaff.QueryAsNoTracking()
                .Include(s => s.Company)
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (staff?.Company?.Status == CompanyStatus.Expired)
            {
                logger.LogWarning("Login blocked for user {UserId}: company expired", user.Id);
                return Errors.Auth.CompanyExpired;
            }
        }

        user.ResetFailedLoginAttempts();

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
            new UserDto(user.Id, user.Email, user.FullName, user.Role.ToString(), user.IsEmailVerified, user.MustChangePassword));
    }
}
