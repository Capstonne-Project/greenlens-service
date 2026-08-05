using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications;

/// <summary>
/// Notifies the officer who assigned a cleanup task when the team accepts, declines,
/// updates progress, or completes their assignment.
/// </summary>
/// <remarks>
/// Implements: BR-CLN-001, BR-CLN-004, BR-CLN-005, BR-CLN-007, BR-NTF-002.
/// </remarks>
public interface ICleanupAssignmentActivityNotifier
{
    Task NotifyAcceptedAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        CancellationToken ct = default);

    Task NotifyDeclinedAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        string declineReason,
        CancellationToken ct = default);

    Task NotifyProgressUpdatedAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        int progressPercent,
        CancellationToken ct = default);

    Task NotifyCompletedAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        bool reportFullyResolved,
        CancellationToken ct = default);
}

public sealed class CleanupAssignmentActivityNotifier(
    INotificationService notificationService,
    IEnvironmentalTeamRepository teams,
    IApplicationDbContext db,
    ILogger<CleanupAssignmentActivityNotifier> logger) : ICleanupAssignmentActivityNotifier
{
    public Task NotifyAcceptedAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        CancellationToken ct = default) =>
        SendAsync(
            assignerUserId,
            teamId,
            reportId,
            reportCode,
            NotificationType.CleanupTaskAccepted,
            teamName => NotificationPlaceholders.ForCleanupTaskAccepted(reportCode, teamName),
            "accepted",
            ct);

    public Task NotifyDeclinedAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        string declineReason,
        CancellationToken ct = default) =>
        SendAsync(
            assignerUserId,
            teamId,
            reportId,
            reportCode,
            NotificationType.CleanupTaskDeclined,
            teamName => NotificationPlaceholders.ForCleanupTaskDeclined(
                reportCode,
                teamName,
                declineReason),
            "declined",
            ct);

    public Task NotifyProgressUpdatedAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        int progressPercent,
        CancellationToken ct = default) =>
        SendAsync(
            assignerUserId,
            teamId,
            reportId,
            reportCode,
            NotificationType.CleanupProgressUpdated,
            teamName => NotificationPlaceholders.ForCleanupProgressUpdated(
                reportCode,
                teamName,
                progressPercent),
            "progress updated",
            ct);

    public Task NotifyCompletedAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        bool reportFullyResolved,
        CancellationToken ct = default) =>
        SendAsync(
            assignerUserId,
            teamId,
            reportId,
            reportCode,
            NotificationType.CleanupTaskCompleted,
            teamName => NotificationPlaceholders.ForCleanupTaskCompleted(
                reportCode,
                teamName,
                reportFullyResolved),
            "completed",
            ct);

    private async Task SendAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        NotificationType type,
        Func<string, Dictionary<string, string>> buildPlaceholders,
        string activityLabel,
        CancellationToken ct)
    {
        var teamName = await ResolveTeamNameAsync(teamId, ct).ConfigureAwait(false);
        var placeholders = buildPlaceholders(teamName);
        placeholders = await NotificationLocalityQueries
            .EnrichFromReportIdAsync(db, placeholders, reportId, ct)
            .ConfigureAwait(false);

        await notificationService.SendFromTemplateAsync(
            assignerUserId,
            type,
            placeholders,
            reportId,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Notified assigner {UserId} that team {TeamId} {Activity} report {ReportCode}",
            assignerUserId, teamId, activityLabel, reportCode);
    }

    private async Task<string> ResolveTeamNameAsync(Guid teamId, CancellationToken ct)
    {
        var team = await teams.GetByIdAsync(teamId, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("Cleanup activity notification: team {TeamId} not found", teamId);
            return "đội xử lý";
        }

        return team.Name;
    }
}
