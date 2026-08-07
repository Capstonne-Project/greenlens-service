using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications;

/// <summary>Notifies inspection team members when a task is assigned (BR-INS-001).</summary>
/// <remarks>Implements: BR-INS-001, BR-NTF-002.</remarks>
public interface IInspectionTaskAssignedNotifier
{
    /// <param name="reportId">Dùng để enrich địa danh (ward) — không dùng làm referenceId.</param>
    /// <param name="inspectionId">
    /// referenceId thật gửi cho client — mobile Inspector route cần InspectionId,
    /// không phải ReportId, để gọi đúng GET /v1/inspections/{id}.
    /// </param>
    Task NotifyTeamAsync(
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
        string reportCode,
        CancellationToken ct = default);
}

public sealed class InspectionTaskAssignedNotifier(
    INotificationService notificationService,
    IEnvironmentalTeamRepository teams,
    ITeamMemberRecipientQuery teamRecipients,
    IApplicationDbContext db,
    ILogger<InspectionTaskAssignedNotifier> logger) : IInspectionTaskAssignedNotifier
{
    public async Task NotifyTeamAsync(
        Guid teamId,
        Guid reportId,
        Guid inspectionId,
        string reportCode,
        CancellationToken ct = default)
    {
        var team = await teams.GetByIdAsync(teamId, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("InspectionTaskAssigned skipped: team {TeamId} not found", teamId);
            return;
        }

        var memberIds = await teamRecipients
            .GetActiveMemberUserIdsAsync(teamId, ct)
            .ConfigureAwait(false);

        if (memberIds.Count == 0)
        {
            logger.LogWarning(
                "InspectionTaskAssigned skipped: no active members for team {TeamId}",
                teamId);
            return;
        }

        var placeholders = NotificationPlaceholders.ForInspectionTaskAssigned(reportCode, team.Name);
        placeholders = await NotificationLocalityQueries
            .EnrichFromReportIdAsync(db, placeholders, reportId, ct)
            .ConfigureAwait(false);

        foreach (var memberId in memberIds)
        {
            await notificationService.SendFromTemplateAsync(
                memberId,
                NotificationType.InspectionTaskAssigned,
                placeholders,
                inspectionId,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Notified {Count} inspector(s) of team {TeamId} about inspection task for report {ReportCode}",
            memberIds.Count, teamId, reportCode);
    }
}
