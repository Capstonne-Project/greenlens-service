using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications;

/// <summary>Notifies all active team members when a cleanup task is assigned (BR-CLN-001).</summary>
/// <remarks>Implements: BR-CLN-001, BR-NTF-002.</remarks>
public interface ICleanupTaskAssignedNotifier
{
    Task NotifyTeamAsync(Guid teamId, Guid reportId, string reportCode, CancellationToken ct = default);
}

public sealed class CleanupTaskAssignedNotifier(
    INotificationService notificationService,
    IEnvironmentalTeamRepository teams,
    ITeamMemberRecipientQuery teamRecipients,
    ILogger<CleanupTaskAssignedNotifier> logger) : ICleanupTaskAssignedNotifier
{
    public async Task NotifyTeamAsync(
        Guid teamId,
        Guid reportId,
        string reportCode,
        CancellationToken ct = default)
    {
        var team = await teams.GetByIdAsync(teamId, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("CleanupTaskAssigned skipped: team {TeamId} not found", teamId);
            return;
        }

        var memberIds = await teamRecipients
            .GetActiveMemberUserIdsAsync(teamId, ct)
            .ConfigureAwait(false);

        if (memberIds.Count == 0)
        {
            logger.LogWarning(
                "CleanupTaskAssigned skipped: no active members for team {TeamId}",
                teamId);
            return;
        }

        var placeholders = NotificationPlaceholders.ForCleanupTaskAssigned(
            reportCode,
            team.Name);

        foreach (var memberId in memberIds)
        {
            await notificationService.SendFromTemplateAsync(
                memberId,
                NotificationType.CleanupTaskAssigned,
                placeholders,
                reportId,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Notified {Count} member(s) of team {TeamId} about report {ReportCode}",
            memberIds.Count, teamId, reportCode);
    }
}
