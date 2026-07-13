using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DuplicateDetection.EventHandlers;

/// <summary>
/// When Tier 1 flags a possible duplicate, enqueue the Tier 2 AI image compare as a
/// background job so report submission is not blocked by the (up to 5s) AI call.
/// </summary>
/// <remarks>Implements: BR-REP-030, BR-AI-002 (Tier 2 image similarity), BR-SYS-001 (keep submit fast).</remarks>
internal sealed class EnqueueDuplicateCompareHandler(
    IDuplicateCompareScheduler scheduler,
    ILogger<EnqueueDuplicateCompareHandler> logger)
    : INotificationHandler<ReportPossibleDuplicateFlaggedEvent>
{
    public Task Handle(ReportPossibleDuplicateFlaggedEvent notification, CancellationToken ct)
    {
        logger.LogInformation(
            "Report {ReportId} flagged possible duplicate of {CandidateId} → enqueue Tier 2 AI compare",
            notification.ReportId, notification.CandidateReportId);

        scheduler.Enqueue(notification.ReportId, notification.CandidateReportId);
        return Task.CompletedTask;
    }
}
