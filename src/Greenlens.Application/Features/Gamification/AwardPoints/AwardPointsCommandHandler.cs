using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.AwardPoints;

/// <summary>
/// Awards points to a user. Idempotent — skips if same ReportId+Reason already recorded.
/// Creates UserPoints record on first interaction (lazy init).
/// </summary>
/// <remarks>Implements: BR-GAM-001, BR-GAM-006 (lock check).</remarks>
public sealed class AwardPointsCommandHandler(
    IUserPointsRepository userPointsRepo,
    IUnitOfWork unitOfWork,
    IChangeTrackerCleaner changeTrackerCleaner,
    ILogger<AwardPointsCommandHandler> logger)
    : IRequestHandler<AwardPointsCommand, Result<AwardPointsResponse>>
{
    public async Task<Result<AwardPointsResponse>> Handle(
        AwardPointsCommand request, CancellationToken ct)
    {
        changeTrackerCleaner.ClearTrackedEntities();

        var userPoints = await userPointsRepo
            .GetOrCreateByUserIdAsync(request.UserId, ct)
            .ConfigureAwait(false);

        var tx = userPoints.AwardPoints(request.Points, request.Reason, request.ReportId);

        if (tx is null)
        {
            logger.LogDebug(
                "Points skipped for user {UserId}: reason={Reason}, reportId={ReportId} (locked or duplicate)",
                request.UserId, request.Reason, request.ReportId);

            return new AwardPointsResponse(0, userPoints.TotalPoints, userPoints.Level, WasSkipped: true);
        }

        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Awarded {Points} points to user {UserId} for {Reason} (report={ReportId}). Total={Total}, Level={Level}",
            request.Points, request.UserId, request.Reason, request.ReportId,
            userPoints.TotalPoints, userPoints.Level);

        return new AwardPointsResponse(
            request.Points, userPoints.TotalPoints, userPoints.Level, WasSkipped: false);
    }
}
