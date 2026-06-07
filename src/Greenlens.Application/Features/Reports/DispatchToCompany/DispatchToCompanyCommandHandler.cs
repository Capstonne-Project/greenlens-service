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
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Verified)
            return Errors.Reports.InvalidStatusTransition;

        // Prevent double-dispatch
        if (report.AssignedCompanyId.HasValue)
            return Errors.Reports.ReportAlreadyDispatchedToCompany;

        var company = await companies.GetByIdAsync(request.CompanyId, ct).ConfigureAwait(false);
        if (company is null)
            return Errors.Reports.CompanyNotFound;

        // BR-CMP-005: company must be active and within contract window
        if (!company.IsWithinContractWindow(DateTime.UtcNow))
            return Errors.Reports.CompanyNotActive;

        // BR-CMP-008: company must serve the ward where the report is located
        if (!string.IsNullOrEmpty(report.WardCode))
        {
            var servesWard = await companies.ServesWardAsync(
                request.CompanyId, report.WardCode, ct).ConfigureAwait(false);

            if (!servesWard)
                return Errors.Reports.CompanyDoesNotServeWard;
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
