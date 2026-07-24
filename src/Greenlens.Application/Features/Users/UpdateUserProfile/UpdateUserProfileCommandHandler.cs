using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Users.UpdateUserProfile;

/// <summary>
/// User updates their own profile (name, phone number). Token-based — no userId param.
/// </summary>
public sealed class UpdateUserProfileCommandHandler(
    IUserRepository users,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<UpdateUserProfileCommandHandler> logger)
    : IRequestHandler<UpdateUserProfileCommand, Result<UpdateUserProfileResponse>>
{
    public async Task<Result<UpdateUserProfileResponse>> Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for ID {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        user.UpdateProfile(request.FullName);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User {UserId} updated their profile", currentUser.UserId);

        return new UpdateUserProfileResponse(user.Id, "Cập nhật hồ sơ thành công.");
    }
}
