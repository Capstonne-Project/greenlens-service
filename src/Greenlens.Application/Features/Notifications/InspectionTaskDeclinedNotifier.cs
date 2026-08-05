using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications;

/// <summary>Notifies LEO when an inspection team declines a task (BR-INS-003).</summary>
/// <remarks>Implements: BR-INS-003, BR-NTF-002. Delegates to <see cref="IInspectionAssignmentActivityNotifier"/>.</remarks>
public interface IInspectionTaskDeclinedNotifier
{
    Task NotifyLeoAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        string declineReason,
        CancellationToken ct = default);
}

public sealed class InspectionTaskDeclinedNotifier(
    IInspectionAssignmentActivityNotifier activityNotifier,
    ILogger<InspectionTaskDeclinedNotifier> logger) : IInspectionTaskDeclinedNotifier
{
    public async Task NotifyLeoAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        string declineReason,
        CancellationToken ct = default)
    {
        await activityNotifier.NotifyDeclinedAsync(
            leoUserId,
            teamId,
            reportId,
            reportCode,
            declineReason,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Notified LEO {UserId} that inspection team {TeamId} declined report {ReportCode}",
            leoUserId, teamId, reportCode);
    }
}
