using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications;

/// <summary>
/// Notifies the LEO who created an inspection dossier when the assigned team accepts,
/// declines, updates field progress, or completes the task.
/// </summary>
/// <remarks>
/// Implements: BR-INS-001, BR-INS-003, BR-INS-012, BR-INS-013, BR-INS-033, BR-NTF-002.
/// </remarks>
public interface IInspectionAssignmentActivityNotifier
{
    /// <param name="reportId">Dùng để enrich địa danh (ward) — không dùng làm referenceId.</param>
    /// <param name="inspectionId">
    /// referenceId thật gửi cho client — mobile Inspector route cần InspectionId.
    /// </param>
    Task NotifyAcceptedAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
        string reportCode,
        CancellationToken ct = default);

    Task NotifyDeclinedAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
        string reportCode,
        string declineReason,
        CancellationToken ct = default);

    Task NotifyProgressUpdatedAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
        string reportCode,
        string activitySummary,
        CancellationToken ct = default);

    Task NotifyCompletedAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
        string reportCode,
        string outcomeSummary,
        CancellationToken ct = default);
}

public sealed class InspectionAssignmentActivityNotifier(
    INotificationService notificationService,
    IEnvironmentalTeamRepository teams,
    IApplicationDbContext db,
    ILogger<InspectionAssignmentActivityNotifier> logger) : IInspectionAssignmentActivityNotifier
{
    public Task NotifyAcceptedAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
        string reportCode,
        CancellationToken ct = default) =>
        SendAsync(
            leoUserId,
            teamId,
            reportId,
            inspectionId,
            reportCode,
            NotificationType.InspectionTaskAccepted,
            teamName => NotificationPlaceholders.ForInspectionTaskAccepted(reportCode, teamName),
            "accepted",
            ct);

    public Task NotifyDeclinedAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
        string reportCode,
        string declineReason,
        CancellationToken ct = default) =>
        SendAsync(
            leoUserId,
            teamId,
            reportId,
            inspectionId,
            reportCode,
            NotificationType.InspectionTaskDeclined,
            teamName => NotificationPlaceholders.ForInspectionTaskDeclined(
                reportCode,
                teamName,
                declineReason),
            "declined",
            ct);

    public Task NotifyProgressUpdatedAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
        string reportCode,
        string activitySummary,
        CancellationToken ct = default) =>
        SendAsync(
            leoUserId,
            teamId,
            reportId,
            inspectionId,
            reportCode,
            NotificationType.InspectionProgressUpdated,
            teamName => NotificationPlaceholders.ForInspectionProgressUpdated(
                reportCode,
                teamName,
                activitySummary),
            "progress updated",
            ct);

    public Task NotifyCompletedAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
        string reportCode,
        string outcomeSummary,
        CancellationToken ct = default) =>
        SendAsync(
            leoUserId,
            teamId,
            reportId,
            inspectionId,
            reportCode,
            NotificationType.InspectionTaskCompleted,
            teamName => NotificationPlaceholders.ForInspectionTaskCompleted(
                reportCode,
                teamName,
                outcomeSummary),
            "completed",
            ct);

    private async Task SendAsync(
        Guid leoUserId,
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
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
            leoUserId,
            type,
            placeholders,
            inspectionId,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Notified LEO {UserId} that inspection team {TeamId} {Activity} report {ReportCode}",
            leoUserId, teamId, activityLabel, reportCode);
    }

    private async Task<string> ResolveTeamNameAsync(Guid teamId, CancellationToken ct)
    {
        var team = await teams.GetByIdAsync(teamId, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("Inspection activity notification: team {TeamId} not found", teamId);
            return "đội thanh tra";
        }

        return team.Name;
    }
}
