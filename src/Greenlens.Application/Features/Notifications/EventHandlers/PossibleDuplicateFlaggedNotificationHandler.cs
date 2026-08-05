using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications.EventHandlers;

/// <summary>
/// Notifies LEO/DEO when Tier 1 duplicate detection flags a newly submitted report.
/// </summary>
/// <remarks>Implements: BR-REP-030, BR-REP-031 (possible duplicate flag), BR-NTF-002.</remarks>
internal sealed class PossibleDuplicateFlaggedNotificationHandler(
    INotificationService notificationService,
    IReportRepository reports,
    IApplicationDbContext db,
    ILogger<PossibleDuplicateFlaggedNotificationHandler> logger)
    : INotificationHandler<ReportPossibleDuplicateFlaggedEvent>
{
    public async Task Handle(ReportPossibleDuplicateFlaggedEvent notification, CancellationToken ct)
    {
        var report = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.ReportId)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.WardCode,
                r.AssignedOfficeId,
                r.AssignedDepartmentId
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (report is null)
        {
            logger.LogWarning(
                "PossibleDuplicateFlagged notification skipped: report {ReportId} not found",
                notification.ReportId);
            return;
        }

        var primaryCode = await reports.QueryAsNoTracking()
            .Where(r => r.Id == notification.CandidateReportId)
            .Select(r => r.Code)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var recipientIds = await ResolveOfficerIdsAsync(
                report.AssignedOfficeId,
                report.AssignedDepartmentId,
                ct)
            .ConfigureAwait(false);

        if (recipientIds.Count == 0)
        {
            logger.LogWarning(
                "PossibleDuplicateFlagged notification skipped: no LEO/DEO for report {ReportCode}",
                report.Code);
            return;
        }

        var placeholders = NotificationPlaceholders.ForDuplicateReviewFromTier1Geo(
            report.Code,
            primaryCode ?? "báo cáo hiện có");
        placeholders = await NotificationLocalityQueries
            .EnrichFromWardCodeAsync(db, placeholders, report.WardCode, ct)
            .ConfigureAwait(false);

        foreach (var recipientId in recipientIds)
        {
            await notificationService.SendFromTemplateAsync(
                recipientId,
                NotificationType.DuplicateReviewNeeded,
                placeholders,
                report.Id,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Notified {Count} officer(s) about Tier 1 possible duplicate on report {ReportCode}",
            recipientIds.Count, report.Code);
    }

    private async Task<List<Guid>> ResolveOfficerIdsAsync(
        Guid? assignedOfficeId,
        Guid? assignedDepartmentId,
        CancellationToken ct)
    {
        if (assignedOfficeId.HasValue)
        {
            return await db.Set<User>()
                .AsNoTracking()
                .Where(u => u.Role == UserRole.LEO
                            && u.LocalOfficeId == assignedOfficeId
                            && !u.IsBanned)
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        if (assignedDepartmentId.HasValue)
        {
            return await db.Set<User>()
                .AsNoTracking()
                .Where(u => u.Role == UserRole.DEO
                            && u.DepartmentId == assignedDepartmentId
                            && !u.IsBanned)
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
        }

        return [];
    }
}
