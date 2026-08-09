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
}

public sealed class CleanupAssignmentActivityNotifier(
    INotificationService notificationService,
    IEnvironmentalTeamRepository teams,
    IEnvironmentalServiceCompanyRepository companies,
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
        var (teamName, companyName) = await ResolveTeamInfoAsync(teamId, ct).ConfigureAwait(false);

        // 1. Always notify the assigner (LEO for community teams, CM for company teams)
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

        // 2. Company team → also notify the LEO who verified/dispatched the report
        if (companyName is not null)
        {
            var leoId = await GetVerifyingLeoIdAsync(reportId, ct).ConfigureAwait(false);
            if (leoId.HasValue && leoId.Value != assignerUserId)
            {
                var teamNameWithCompany = $"{teamName} ({companyName})";
                var leoPlaceholders = buildPlaceholders(teamNameWithCompany);
                leoPlaceholders = await NotificationLocalityQueries
                    .EnrichFromReportIdAsync(db, leoPlaceholders, reportId, ct)
                    .ConfigureAwait(false);

                await notificationService.SendFromTemplateAsync(
                    leoId.Value,
                    type,
                    leoPlaceholders,
                    reportId,
                    ct).ConfigureAwait(false);

                logger.LogInformation(
                    "Notified LEO {LeoId} that company team {TeamId} ({CompanyName}) {Activity} report {ReportCode}",
                    leoId.Value, teamId, companyName, activityLabel, reportCode);
            }
        }
    }

    /// <summary>
    /// Resolves team name and, if it's a company team, the company name.
    /// Returns (teamName, null) for community teams, (teamName, companyName) for company teams.
    /// </summary>
    private async Task<(string teamName, string? companyName)> ResolveTeamInfoAsync(
        Guid teamId, CancellationToken ct)
    {
        var team = await teams.GetByIdAsync(teamId, ct).ConfigureAwait(false);
        if (team is null)
        {
            logger.LogWarning("Cleanup activity notification: team {TeamId} not found", teamId);
            return ("đội xử lý", null);
        }

        if (!team.IsCompanyTeam)
            return (team.Name, null);

        var company = await companies.GetByIdAsync(team.CompanyId!.Value, ct).ConfigureAwait(false);
        return (team.Name, company?.Name ?? "công ty");
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

