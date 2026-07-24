using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.AcceptDataConsent;

/// <summary>
/// Records explicit user consent for data processing (photos, GPS).
/// Idempotent — calling again when already accepted is a no-op success.
/// </summary>
/// <remarks>Implements: BR-DAT-005.</remarks>
public sealed class AcceptDataConsentCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<AcceptDataConsentCommandHandler> logger)
    : IRequestHandler<AcceptDataConsentCommand, Result>
{
    public async Task<Result> Handle(AcceptDataConsentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Accepting data processing consent for user {UserId}", currentUser.UserId);

        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for ID {UserId}", currentUser.UserId);
            return Errors.Auth.UserNotFound;
        }

        if (user.HasDataConsent)
        {
            logger.LogInformation("User {UserId} already accepted data processing consent", currentUser.UserId);
            return Result.Success(); // idempotent
        }

        user.AcceptDataConsent();

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User {UserId} accepted data processing consent", user.Id);

        return Result.Success();
    }
}
