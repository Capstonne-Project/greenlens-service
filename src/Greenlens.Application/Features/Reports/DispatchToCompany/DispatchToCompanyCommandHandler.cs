using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DispatchToCompany;

/// <summary>
/// LEO dispatches a verified report to a company. Report stays Verified.
/// CompanyManager sees it in their queue and assigns company team(s).
/// </summary>
/// <remarks>Implements: BR-CMP-005, BR-OFF-011.</remarks>
public sealed class DispatchToCompanyCommandHandler(
    IReportRepository reports,
    IEnvironmentalServiceCompanyRepository companies,
    ICurrentUser currentUser,
    IUnitOfWork uow,
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

        if (report.Status != ReportStatus.Verified)
        {
            logger.LogWarning("Report {ReportId} is not verified", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        // Prevent double-dispatch
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

        // BR-CMP-005: company must be active (contract dates are metadata only, not routing gate)
        if (!company.IsActive)
        {
            logger.LogWarning("Company {CompanyId} is not active", request.CompanyId);
            return Errors.Reports.CompanyNotActive;
        }

        // BR-CMP-008: company must serve the ward where the report is located
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

        // Dispatch — report stays Verified, AssignedCompanyId set
        report.DispatchToCompany(request.CompanyId, currentUser.UserId);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportId} dispatched to Company {CompanyId} by LEO {UserId}",
            report.Id, request.CompanyId, currentUser.UserId);

        return Result.Success();
    }
}
