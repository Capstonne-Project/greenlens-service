using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Reports.GetDispatchableCompanies;

/// <summary>
/// Returns active companies whose service area includes the report's ward.
/// LEO uses this list to pick which company receives the cleanup task.
/// </summary>
/// <remarks>Implements: BR-CMP-005 (active check), BR-CMP-008 (service area match).</remarks>
public sealed class GetDispatchableCompaniesQueryHandler(
    IReportRepository reports,
    IEnvironmentalServiceCompanyRepository companies)
    : IRequestHandler<GetDispatchableCompaniesQuery, Result<GetDispatchableCompaniesResponse>>
{
    public async Task<Result<GetDispatchableCompaniesResponse>> Handle(
        GetDispatchableCompaniesQuery request, CancellationToken ct)
    {
        // 1. Load report to get WardCode
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (string.IsNullOrEmpty(report.WardCode))
            return new GetDispatchableCompaniesResponse([]);

        // 2. Find active companies serving this ward
        var items = await companies.QueryAsNoTracking()
            .Include(c => c.ServiceAreas)
            .Include(c => c.Staff)
            .Where(c => c.Status == CompanyStatus.Active)
            .Where(c => c.ServiceAreas.Any(sa => sa.WardCode == report.WardCode))
            .OrderBy(c => c.Name)
            .Select(c => new DispatchableCompanyItem(
                c.Id,
                c.Name,
                c.ContractNumber,
                c.ContractType.ToString(),
                c.Status.ToString(),
                c.Phone,
                c.Email,
                c.ServiceAreas.Count,
                c.Staff.Count))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetDispatchableCompaniesResponse(items);
    }
}
