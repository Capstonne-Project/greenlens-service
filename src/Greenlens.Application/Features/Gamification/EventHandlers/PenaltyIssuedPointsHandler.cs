using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Gamification.EventHandlers;

/// <summary>
/// Awards points to the citizen reporter when a penalty decision is issued on their report.
/// </summary>
/// <remarks>Implements: BR-GAM-001, BR-ADM-005.</remarks>
public sealed class PenaltyIssuedPointsHandler(
    ISender sender,
    IApplicationDbContext db,
    IReportRepository reports,
    ILogger<PenaltyIssuedPointsHandler> logger)
    : INotificationHandler<PenaltyIssuedEvent>
{
    public async Task Handle(PenaltyIssuedEvent notification, CancellationToken ct)
    {
        var reporterId = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.ReportId)
            .Select(r => r.ReporterId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (!reporterId.HasValue)
        {
            logger.LogDebug(
                "PenaltyIssued points skipped: report {ReportId} has no reporter",
                notification.ReportId);
            return;
        }

        var points = await GamificationPointAwarder
            .GetConfiguredPointsAsync(db, PointReason.PenaltyIssued, 20, ct)
            .ConfigureAwait(false);

        if (points == 0)
            return;

        await GamificationPointAwarder.TryAwardAsync(
            sender,
            logger,
            reporterId.Value,
            points,
            PointReason.PenaltyIssued,
            notification.ReportId,
            "PenaltyIssued",
            checkBadges: true,
            ct).ConfigureAwait(false);
    }
}
