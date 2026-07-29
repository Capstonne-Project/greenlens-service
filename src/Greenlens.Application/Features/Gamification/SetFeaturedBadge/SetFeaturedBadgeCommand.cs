using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.SetFeaturedBadge;

/// <summary>
/// Người dùng chọn 1 huy hiệu đã đạt để hiển thị nổi bật trên hồ sơ.
/// Truyền <c>BadgeId = null</c> để bỏ hiển thị.
/// </summary>
/// <remarks>Implements: BR-GAM-004.</remarks>
public sealed record SetFeaturedBadgeCommand(Guid UserId, Guid? BadgeId)
    : IRequest<Result<SetFeaturedBadgeResponse>>;

public sealed record SetFeaturedBadgeResponse(Guid? FeaturedBadgeId);

public sealed class SetFeaturedBadgeCommandHandler(
    IUserRepository userRepo,
    IUserBadgeRepository userBadgeRepo,
    IUnitOfWork unitOfWork,
    ILogger<SetFeaturedBadgeCommandHandler> logger)
    : IRequestHandler<SetFeaturedBadgeCommand, Result<SetFeaturedBadgeResponse>>
{
    public async Task<Result<SetFeaturedBadgeResponse>> Handle(
        SetFeaturedBadgeCommand request, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(request.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("User {UserId} not found", request.UserId);
            return Errors.Users.UserNotFound;
        }

        // Chỉ cho phép chọn huy hiệu user thực sự đã đạt được
        if (request.BadgeId is not null)
        {
            var owned = await userBadgeRepo
                .HasBadgeAsync(request.UserId, request.BadgeId.Value, ct)
                .ConfigureAwait(false);

            if (!owned)
            {
                logger.LogWarning(
                    "User {UserId} tried to feature unowned badge {BadgeId}",
                    request.UserId, request.BadgeId);
                return Errors.Gamification.BadgeNotOwned;
            }
        }

        user.SetFeaturedBadge(request.BadgeId);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "User {UserId} featured badge set to {BadgeId}", request.UserId, request.BadgeId);

        return new SetFeaturedBadgeResponse(request.BadgeId);
    }
}
