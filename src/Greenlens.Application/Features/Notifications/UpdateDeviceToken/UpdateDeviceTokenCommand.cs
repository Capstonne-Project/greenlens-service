using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.UpdateDeviceToken;

/// <summary>Register or update FCM device token for push notifications (BR-NTF-001).</summary>
public sealed record UpdateDeviceTokenCommand(string? DeviceToken) : IRequest<Result>;

/// <remarks>
/// Called by the mobile app on startup to register/refresh the FCM token.
/// Passing null clears the token (opt-out of push).
/// </remarks>
internal sealed class UpdateDeviceTokenCommandHandler(
    ICurrentUser currentUser,
    IUserRepository userRepo,
    IUnitOfWork unitOfWork,
    ILogger<UpdateDeviceTokenCommandHandler> logger)
    : IRequestHandler<UpdateDeviceTokenCommand, Result>
{
    public async Task<Result> Handle(UpdateDeviceTokenCommand request, CancellationToken ct)
    {
        logger.LogInformation("Updating device token");

        var user = await userRepo.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for user ID {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        user.UpdateFcmToken(request.DeviceToken);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Device token updated for user ID {UserId}", currentUser.UserId);

        return Result.Success();
    }
}
