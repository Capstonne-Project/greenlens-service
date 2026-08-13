using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications;

/// <summary>
/// Notifies LEO and company managers when a citizen closes a resolved report (BR-REP-016).
/// </summary>
public interface IReportClosedByCitizenNotifier
{
    Task NotifyAsync(Report report, CancellationToken ct = default);
}

public sealed class ReportClosedByCitizenNotifier(
    INotificationService notificationService,
    ICompanyManagerRecipientQuery companyManagers,
    IApplicationDbContext db,
    ILogger<ReportClosedByCitizenNotifier> logger) : IReportClosedByCitizenNotifier
{
    public async Task NotifyAsync(Report report, CancellationToken ct = default)
    {
        var placeholders = NotificationPlaceholders.ForReportClosedByCitizen(report.Code);
        placeholders = await NotificationLocalityQueries
            .EnrichFromReportIdAsync(db, placeholders, report.Id, ct)
            .ConfigureAwait(false);

        var notifiedUserIds = new HashSet<Guid>();

        if (report.VerifiedBy.HasValue && notifiedUserIds.Add(report.VerifiedBy.Value))
        {
            await notificationService.SendFromTemplateAsync(
                report.VerifiedBy.Value,
                NotificationType.ReportClosedByCitizen,
                placeholders,
                report.Id,
                ct).ConfigureAwait(false);

            logger.LogInformation(
                "Notified LEO {LeoId} that citizen closed report {ReportCode}",
                report.VerifiedBy.Value, report.Code);
        }
        else if (report.AssignedOfficeId.HasValue)
        {
            var leoIds = await db.Set<User>()
                .AsNoTracking()
                .Where(u => u.Role == UserRole.LEO
                            && u.LocalOfficeId == report.AssignedOfficeId
                            && !u.IsBanned)
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var leoId in leoIds)
            {
                if (!notifiedUserIds.Add(leoId))
                    continue;

                await notificationService.SendFromTemplateAsync(
                    leoId,
                    NotificationType.ReportClosedByCitizen,
                    placeholders,
                    report.Id,
                    ct).ConfigureAwait(false);
            }
        }

        if (!report.AssignedCompanyId.HasValue)
            return;

        var managerIds = await companyManagers
            .GetActiveManagerIdsByCompanyAsync(report.AssignedCompanyId.Value, ct)
            .ConfigureAwait(false);

        foreach (var managerId in managerIds)
        {
            if (!notifiedUserIds.Add(managerId))
                continue;

            await notificationService.SendFromTemplateAsync(
                managerId,
                NotificationType.ReportClosedByCitizen,
                placeholders,
                report.Id,
                ct).ConfigureAwait(false);

            logger.LogInformation(
                "Notified CompanyManager {ManagerId} that citizen closed report {ReportCode}",
                managerId, report.Code);
        }
    }
}
