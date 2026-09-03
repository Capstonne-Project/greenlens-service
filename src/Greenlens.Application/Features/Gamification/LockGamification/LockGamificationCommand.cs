using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.LockGamification;

/// <summary>
/// BR-GAM-006: Admin locks a user's gamification for fraud.
/// Deducts all points and blocks accumulation for 30 days.
/// </summary>
public sealed record LockGamificationCommand(
    Guid TargetUserId,
    string Reason,
    int LockDays = 30) : IRequest<Result<LockGamificationResponse>>;

public sealed record LockGamificationResponse(
    int PointsDeducted,
    DateTime LockedUntil);

public sealed class LockGamificationCommandHandler(
    IUserRepository userRepo,
    IUserPointsRepository userPointsRepo,
    IUnitOfWork unitOfWork,
    ILeaderboardCache leaderboardCache,
    ILogger<LockGamificationCommandHandler> logger)
    : IRequestHandler<LockGamificationCommand, Result<LockGamificationResponse>>
{
    public async Task<Result<LockGamificationResponse>> Handle(
        LockGamificationCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting lock gamification");

        // Validate target user exists before creating UserPoints (FK constraint)
        var user = await userRepo.GetByIdAsync(request.TargetUserId, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User {UserId} not found", request.TargetUserId);
            return Errors.Users.UserNotFound;
        }

        var userPoints = await userPointsRepo
            .GetOrCreateByUserIdAsync(request.TargetUserId, ct)
            .ConfigureAwait(false);

        if (userPoints.IsLocked)
        {
            logger.LogWarning("User {UserId} already locked", request.TargetUserId);
            return Errors.Gamification.AlreadyLocked;
        }

        var deducted = userPoints.Lock(request.Reason, request.LockDays);

        logger.LogInformation("Gamification locked for user {UserId}. Deducted {Points} points. Reason: {Reason}. Until: {Until}", request.TargetUserId, deducted, request.Reason, userPoints.LockedUntil);

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        // Lock/deduct điểm → ranking thay đổi; invalidate leaderboard cache (BR-GAM-005).
        await leaderboardCache.InvalidateAsync(ct).ConfigureAwait(false);

        return new LockGamificationResponse(deducted, userPoints.LockedUntil!.Value);
    }
}
