using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.RestoreAccount;

/// <summary>
/// Restore a soft-deleted account before the 90-day deadline.
/// Requires email + password since soft-deleted users cannot login.
/// </summary>
/// <remarks>Implements: BR-AUTH-021 (restore before hard delete).</remarks>
public sealed class RestoreAccountCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    IPasswordHasher passwordHasher,
    ILogger<RestoreAccountCommandHandler> logger)
    : IRequestHandler<RestoreAccountCommand, Result<RestoreAccountResponse>>
{
    public async Task<Result<RestoreAccountResponse>> Handle(
        RestoreAccountCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting account restoration");

        // Find the soft-deleted user (bypasses global query filter)
        var user = await users.GetDeletedByEmailAsync(request.Email, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for email {Email}", request.Email);
            return Errors.Auth.UserNotFound;
        }

        // Verify password
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Invalid password for email {Email}", request.Email);
            return Errors.Auth.InvalidCredentials;
        }

        // Restore the account
        user.Restore();

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User {UserId} restored their account", user.Id);

        return new RestoreAccountResponse("Tài khoản đã được khôi phục thành công.");
    }
}
