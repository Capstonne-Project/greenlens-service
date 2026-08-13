using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications;

/// <summary>
/// Notifies the officer who assigned a cleanup task when the team accepts, declines,
/// updates progress, or completes their assignment.
/// For company teams: also notifies the LEO who verified/dispatched the report,
/// with the company name included in the team label.
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

    Task NotifyCheckedInAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        CancellationToken ct = default);

    Task NotifyBeforeImagesUploadedAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        int imageCount,
        CancellationToken ct = default);
}

public sealed class CleanupAssignmentActivityNotifier(
    INotificationService notificationService,
    IEnvironmentalTeamRepository teams,
    IEnvironmentalServiceCompanyRepository companies,
    ICompanyManagerRecipientQuery companyManagers,
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

    public Task NotifyCheckedInAsync(
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
            NotificationType.CleanupTeamCheckedIn,
            teamName => NotificationPlaceholders.ForCleanupTeamCheckedIn(reportCode, teamName),
            "checked in",
            ct);

    public Task NotifyBeforeImagesUploadedAsync(
        Guid assignerUserId,
        Guid teamId,
        Guid reportId,
        string reportCode,
        int imageCount,
        CancellationToken ct = default) =>
        SendAsync(
            assignerUserId,
            teamId,
            reportId,
            reportCode,
            NotificationType.CleanupBeforeImagesUploaded,
            teamName => NotificationPlaceholders.ForCleanupBeforeImagesUploaded(
                reportCode, teamName, imageCount),
            "uploaded before images",
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
        var (teamName, companyName, companyId) = await ResolveTeamInfoAsync(teamId, ct).ConfigureAwait(false);
        var notifiedUserIds = new HashSet<Guid> { assignerUserId };

        await SendToUserAsync(
            assignerUserId,
            teamName,
            reportId,
            reportCode,
            type,
            buildPlaceholders,
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Notified assigner {UserId} that team {TeamId} {Activity} report {ReportCode}",
            assignerUserId, teamId, activityLabel, reportCode);

        if (companyId is null)
            return;

        var teamNameWithCompany = $"{teamName} ({companyName})";

        var leoId = await GetVerifyingLeoIdAsync(reportId, ct).ConfigureAwait(false);
        if (leoId.HasValue && notifiedUserIds.Add(leoId.Value))
        {
            await SendToUserAsync(
                leoId.Value,
                teamNameWithCompany,
                reportId,
                reportCode,
                type,
                buildPlaceholders,
                ct).ConfigureAwait(false);

            logger.LogInformation(
                "Notified LEO {LeoId} that company team {TeamId} ({CompanyName}) {Activity} report {ReportCode}",
                leoId.Value, teamId, companyName, activityLabel, reportCode);
        }

        var managerIds = await companyManagers
            .GetActiveManagerIdsByCompanyAsync(companyId.Value, ct)
            .ConfigureAwait(false);

        foreach (var managerId in managerIds)
        {
            if (!notifiedUserIds.Add(managerId))
                continue;

            await SendToUserAsync(
                managerId,
                teamNameWithCompany,
                reportId,
                reportCode,
                type,
                buildPlaceholders,
                ct).ConfigureAwait(false);

            logger.LogInformation(
                "Notified CompanyManager {ManagerId} that team {TeamId} ({CompanyName}) {Activity} report {ReportCode}",
                managerId, teamId, companyName, activityLabel, reportCode);
        }
    }

    private async Task SendToUserAsync(
        Guid userId,
        string teamName,
        Guid reportId,
        string reportCode,
        NotificationType type,
        Func<string, Dictionary<string, string>> buildPlaceholders,
        CancellationToken ct)
    {
        var placeholders = buildPlaceholders(teamName);
        placeholders = await NotificationLocalityQueries
            .EnrichFromReportIdAsync(db, placeholders, reportId, ct)
            .ConfigureAwait(false);

        await notificationService.SendFromTemplateAsync(
            userId,
            type,
            placeholders,
            reportId,
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves team name and, if it's a company team, the company name and id.
    /// </summary>
    private async Task<(string teamName, string? companyName, Guid? companyId)> ResolveTeamInfoAsync(
        Guid teamId, CancellationToken ct)
    {
        var team = await teams.GetByIdAsync(teamId, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("Cleanup activity notification: team {TeamId} not found", teamId);
            return ("đội xử lý", null, null);
        }

        if (!team.IsCompanyTeam)
            return (team.Name, null, null);

        var company = await companies.GetByIdAsync(team.CompanyId!.Value, ct).ConfigureAwait(false);
        return (team.Name, company?.Name ?? "công ty", team.CompanyId);
    }

    /// <summary>
    /// Gets the LEO who verified the report — typically the same officer who dispatched it.
    /// </summary>
    private async Task<Guid?> GetVerifyingLeoIdAsync(Guid reportId, CancellationToken ct)
    {
        return await db.Set<Report>()
            .AsNoTracking()
            .Where(r => r.Id == reportId)
            .Select(r => r.VerifiedBy)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}

