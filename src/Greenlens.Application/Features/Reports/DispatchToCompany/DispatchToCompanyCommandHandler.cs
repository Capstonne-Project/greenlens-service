using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DispatchToCompany;

/// <summary>
/// LEO dispatches a verified report to a company. Verified → InProgress.
/// CompanyManager sees it in company-queue and assigns company team(s).
/// </summary>
/// <remarks>Implements: BR-CMP-005, BR-OFF-011, BR-NTF-002, BR-ADM-010.</remarks>
public sealed class DispatchToCompanyCommandHandler(
    IReportRepository reports,
    IEnvironmentalServiceCompanyRepository companies,
    IReportStatusHistoryRepository statusHistory,
    INotificationService notifications,
    ICompanyManagerRecipientQuery companyManagers,
    IApplicationDbContext db,
    ICommunityCleanupEventRepository communityCleanupEvents,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<DispatchToCompanyCommandHandler> logger) : IRequestHandler<DispatchToCompanyCommand, Result>
{
    public async Task<Result> Handle(DispatchToCompanyCommand request, CancellationToken ct)
    {
        logger.LogInformation("Dispatching report {ReportId} to company {CompanyId}", request.ReportId, request.CompanyId);

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (report.Status is not (ReportStatus.Verified or ReportStatus.Reopened))
        {
            logger.LogWarning("Report {ReportId} is not ready for company dispatch", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        // Draft BR-CMU-003: a Community Cleanup event takes over the report — block dispatch while active.
        var activeCommunityEvent = await communityCleanupEvents.GetActiveByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (activeCommunityEvent is not null)
        {
            logger.LogWarning("Report {ReportId} has an active community cleanup event", request.ReportId);
            return Errors.CommunityCleanup.CommunityAlreadyActive;
        }

        if (report.AssignedCompanyId.HasValue)
        {
            logger.LogWarning("Report {ReportId} is already dispatched to company {CompanyId}", request.ReportId, request.CompanyId);
            return Errors.Reports.ReportAlreadyDispatchedToCompany;
        }

        var company = await companies.GetByIdAsync(request.CompanyId, ct).ConfigureAwait(false);
        if (company is null)
        {
            logger.LogWarning("Company not found for ID {CompanyId}", request.CompanyId);
            return Errors.Reports.CompanyNotFound;
        }

        if (!company.IsActive)
        {
            logger.LogWarning("Company {CompanyId} is not active", request.CompanyId);
            return Errors.Reports.CompanyNotActive;
        }

        if (!string.IsNullOrEmpty(report.WardCode))
        {
            var servesWard = await companies.ServesWardAsync(
                request.CompanyId, report.WardCode, ct).ConfigureAwait(false);

            if (!servesWard)
            {
                logger.LogWarning("Company {CompanyId} does not serve ward {WardCode}", request.CompanyId, report.WardCode);
                return Errors.Reports.CompanyDoesNotServeWard;
            }
        }

        var fromStatus = report.Status;
        report.DispatchToCompany(request.CompanyId, currentUser.UserId);

        statusHistory.Add(ReportStatusHistory.Create(
            report.Id,
            fromStatus,
            ReportStatus.InProgress,
            currentUser.UserId));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "DispatchToCompany",
            "Report",
            report.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { status = fromStatus.ToString() }),
            newValues: JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                companyId = request.CompanyId
            }),
            ct).ConfigureAwait(false);

        await NotifyCompanyManagersAsync(report, company, ct).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportId} dispatched to Company {CompanyId} by LEO {UserId}",
            report.Id, request.CompanyId, currentUser.UserId);

        return Result.Success();
    }

    private async Task NotifyCompanyManagersAsync(
        Report report,
        EnvironmentalServiceCompany company,
        CancellationToken ct)
    {
        var managerIds = await companyManagers
            .GetActiveManagerIdsByCompanyAsync(company.Id, ct)
            .ConfigureAwait(false);

        if (managerIds.Count == 0)
        {
            logger.LogWarning(
                "CompanyReportDispatched skipped: no active CompanyManager for company {CompanyId}",
                company.Id);
            return;
        }

        var placeholders = NotificationPlaceholders.ForCompanyReportDispatched(
            report.Code,
            company.Name);
        placeholders = await NotificationLocalityQueries
            .EnrichFromReportIdAsync(db, placeholders, report.Id, ct)
            .ConfigureAwait(false);

        foreach (var managerId in managerIds)
        {
            await notifications.SendFromTemplateAsync(
                managerId,
                NotificationType.CompanyReportDispatched,
                placeholders,
                report.Id,
                ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Notified {Count} CompanyManager(s) about report {ReportCode} dispatched to {CompanyName}",
            managerIds.Count, report.Code, company.Name);
    }
}
