using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetCompanyQueue;

/// <summary>
/// Returns reports dispatched to the caller's company that are awaiting team assignment.
/// Filters: Status == Verified AND AssignedCompanyId == caller's companyId.
/// </summary>
public sealed class GetCompanyQueueQueryHandler(
    IReportRepository reports,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    ILogger<GetCompanyQueueQueryHandler> logger) : IRequestHandler<GetCompanyQueueQuery, Result<GetCompanyQueueResponse>>
{
    public async Task<Result<GetCompanyQueueResponse>> Handle(GetCompanyQueueQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting company queue for user {UserId}", currentUser.UserId);

        // Resolve caller's company
        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (staff is null || !staff.IsActive)
        {
            logger.LogWarning("Staff not found for user {UserId}", currentUser.UserId);
            return Errors.Reports.ReportNotDispatchedToYourCompany;
        }

        var companyId = staff.CompanyId;

        logger.LogInformation("Company ID: {CompanyId}", companyId);

        // Query reports dispatched to this company, still Verified (awaiting CM team assignment)
        var baseQuery = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.Status == ReportStatus.Verified
                        && r.AssignedCompanyId == companyId);

        if (request.Severity.HasValue)
        {
            logger.LogInformation("Filtering by severity: {Severity}", request.Severity.Value);
            baseQuery = baseQuery.Where(r => r.Severity == request.Severity.Value);
        }

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var items = await baseQuery
            .OrderByDescending(r => r.PriorityScore)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new CompanyQueueItem(
                r.Id,
                r.Code,
                r.Address,
                r.WardCode,
                r.Latitude,
                r.Longitude,
                r.Category.NameVi,
                r.Severity,
                r.DispatchedToCompanyAt,
                r.SlaResolveDueAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, total);

        logger.LogInformation("CompanyManager {UserId} viewed queue: {Count} reports for company {CompanyId}",
            currentUser.UserId, total, companyId);

        return new GetCompanyQueueResponse(items, pagination);
    }
}
