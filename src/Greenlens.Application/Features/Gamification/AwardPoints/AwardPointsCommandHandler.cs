using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Gamification.CheckBadges;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.AwardPoints;

/// <summary>
/// Awards points to a user. Idempotent — skips if same ReportId+Reason already recorded.
/// Creates UserPoints record on first interaction (lazy init).
/// </summary>
/// <remarks>Implements: BR-GAM-001, BR-GAM-006 (lock check), BR-GAM-004 (level badges after award).</remarks>
public sealed class AwardPointsCommandHandler(
    IUserPointsRepository userPointsRepo,
    IUnitOfWork unitOfWork,
    IChangeTrackerCleaner changeTrackerCleaner,
    ISender sender,
    ILeaderboardCache leaderboardCache,
    ILogger<AwardPointsCommandHandler> logger)
    : IRequestHandler<AwardPointsCommand, Result<AwardPointsResponse>>
{
    public async Task<Result<AwardPointsResponse>> Handle(
        AwardPointsCommand request, CancellationToken ct)
    {
        changeTrackerCleaner.ClearTrackedEntities();

        // DB-level idempotency — in-memory _transactions may miss soft-deleted or unloaded rows.
        if (request.ReportId.HasValue
            && await userPointsRepo.HasTransactionForReportAsync(
                request.UserId, request.ReportId.Value, request.Reason, ct).ConfigureAwait(false))
        {
            logger.LogWarning(
                "Points skipped (duplicate at DB) for user {UserId}: reason={Reason}, reportId={ReportId}",
                request.UserId, request.Reason, request.ReportId);

            var existing = await userPointsRepo.GetByUserIdAsync(request.UserId, ct).ConfigureAwait(false);
            return new AwardPointsResponse(
                0,
                existing?.TotalPoints ?? 0,
                existing?.Level ?? 1,
                WasSkipped: true);
        }

        var userPoints = await userPointsRepo
            .GetOrCreateByUserIdAsync(request.UserId, ct)
            .ConfigureAwait(false);

        var tx = userPoints.AwardPoints(request.Points, request.Reason, request.ReportId);

        if (tx is null)
        {
            logger.LogWarning(
                "Points skipped for user {UserId}: reason={Reason}, reportId={ReportId} (locked or duplicate in memory)",
                request.UserId, request.Reason, request.ReportId);

            return new AwardPointsResponse(0, userPoints.TotalPoints, userPoints.Level, WasSkipped: true);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            // Race or soft-deleted row blocked by unique index — treat as idempotent skip.
            logger.LogWarning(
                ex,
                "Points save conflict for user {UserId}: reason={Reason}, reportId={ReportId}",
                request.UserId, request.Reason, request.ReportId);

            changeTrackerCleaner.ClearTrackedEntities();
            var existing = await userPointsRepo.GetByUserIdAsync(request.UserId, ct).ConfigureAwait(false);
            return new AwardPointsResponse(
                0,
                existing?.TotalPoints ?? 0,
                existing?.Level ?? 1,
                WasSkipped: true);
        }

        logger.LogInformation(
            "Awarded {Points} points to user {UserId} for {Reason} (report={ReportId}). Total={Total}, Level={Level}",
            request.Points, request.UserId, request.Reason, request.ReportId,
            userPoints.TotalPoints, userPoints.Level);

        // Level badges (rising_star…) cần total_points mới — recheck ngay, không đợi BadgeRecheckJob 08:00 ICT.
        changeTrackerCleaner.ClearTrackedEntities();
        await sender.Send(new CheckBadgesCommand(request.UserId), ct).ConfigureAwait(false);

        // BR-GAM-005: điểm đổi → xóa leaderboard cache để lần đọc tiếp theo lấy ranking mới.
        await leaderboardCache.InvalidateAsync(ct).ConfigureAwait(false);

        return new AwardPointsResponse(
            request.Points, userPoints.TotalPoints, userPoints.Level, WasSkipped: false);
    }
}
