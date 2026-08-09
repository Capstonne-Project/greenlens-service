using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Notifications;

/// <summary>
/// Notifies ward LEO(s) when a CompanyManager assigns company team(s) to a dispatched report.
/// </summary>
/// <remarks>Implements: BR-CMP-005, BR-NTF-002.</remarks>
public interface ICompanyTeamAssignedLeoNotifier
{
    Task NotifyAsync(
        Guid reportId,
        string reportCode,
        Guid? assignedOfficeId,
        Guid companyId,
        IReadOnlyList<string> teamNames,
        CancellationToken ct = default);
}

public sealed class CompanyTeamAssignedLeoNotifier(
    INotificationService notificationService,
    IOfficerRecipientQuery officerRecipients,
    IEnvironmentalServiceCompanyRepository companies,
    IApplicationDbContext db,
    ILogger<CompanyTeamAssignedLeoNotifier> logger) : ICompanyTeamAssignedLeoNotifier
{
    public async Task NotifyAsync(
        Guid reportId,
        string reportCode,
        Guid? assignedOfficeId,
        Guid companyId,
        IReadOnlyList<string> teamNames,
        CancellationToken ct = default)
    {
        if (!assignedOfficeId.HasValue)
        {
            logger.LogWarning(
                "CompanyTeamAssigned skipped: report {ReportId} has no AssignedOfficeId",
                reportId);
            return;
        }

        var leoIds = await officerRecipients
            .GetLeoIdsByOfficeAsync(assignedOfficeId.Value, ct)
            .ConfigureAwait(false);

        if (leoIds.Count == 0)
        {
            logger.LogWarning(
                "CompanyTeamAssigned skipped: no LEO for office {OfficeId} (report {ReportId})",
                assignedOfficeId.Value, reportId);
            return;
        }

        var company = await companies.GetByIdAsync(companyId, ct).ConfigureAwait(false);
        var companyName = company?.Name ?? "công ty";
        var teamNamesLabel = teamNames.Count > 0
            ? string.Join(", ", teamNames)
            : "đội xử lý";

        var placeholders = NotificationPlaceholders.ForCompanyTeamAssigned(
            reportCode,
            companyName,
            teamNamesLabel);
        placeholders = await NotificationLocalityQueries
            .EnrichFromReportIdAsync(db, placeholders, reportId, ct)
            .ConfigureAwait(false);

        foreach (var leoId in leoIds)
        {
            await notificationService.SendFromTemplateAsync(
                leoId,
                NotificationType.CompanyTeamAssigned,
                placeholders,
                reportId,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Notified {Count} LEO(s) in office {OfficeId} that company {CompanyId} assigned teams to report {ReportCode}",
            leoIds.Count, assignedOfficeId.Value, companyId, reportCode);
    }
}
