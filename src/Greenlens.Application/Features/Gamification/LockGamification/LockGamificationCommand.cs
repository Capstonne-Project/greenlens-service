using Greenlens.Application.Common;
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
    IUserPointsRepository userPointsRepo,
    IUnitOfWork unitOfWork,
    ILogger<LockGamificationCommandHandler> logger)
    : IRequestHandler<LockGamificationCommand, Result<LockGamificationResponse>>
{
    public async Task<Result<LockGamificationResponse>> Handle(
        LockGamificationCommand request, CancellationToken ct)
    {
        var userPoints = await userPointsRepo
            .GetOrCreateByUserIdAsync(request.TargetUserId, ct)
            .ConfigureAwait(false);

        if (userPoints.IsLocked)
            return Errors.Gamification.AlreadyLocked;

        var deducted = userPoints.Lock(request.Reason, request.LockDays);

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogWarning(
            "BR-GAM-006: Gamification locked for user {UserId}. Deducted {Points} points. Reason: {Reason}. Until: {Until}",
            request.TargetUserId, deducted, request.Reason, userPoints.LockedUntil);

        return new LockGamificationResponse(deducted, userPoints.LockedUntil!.Value);
    }
}
