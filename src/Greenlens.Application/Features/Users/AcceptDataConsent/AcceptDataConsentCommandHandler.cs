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
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        if (user.HasDataConsent)
            return Result.Success(); // idempotent

        user.AcceptDataConsent();

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User {UserId} accepted data processing consent", user.Id);

        return Result.Success();
    }
}
