using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.GetAdminReports;

public sealed class GetAdminReportsQueryHandler(
    IReportRepository reports)
    : IRequestHandler<GetAdminReportsQuery, Result<GetAdminReportsResponse>>
{
    public async Task<Result<GetAdminReportsResponse>> Handle(
        GetAdminReportsQuery request, CancellationToken ct)
    {
        var query = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Assignments)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);
        if (request.CategoryId.HasValue)
            query = query.Where(r => r.CategoryId == request.CategoryId.Value);
        if (!string.IsNullOrEmpty(request.WardCode))
            query = query.Where(r => r.WardCode == request.WardCode);
        if (!string.IsNullOrEmpty(request.ProvinceCode))
            query = query.Where(r => r.ProvinceCode == request.ProvinceCode);
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(r => r.Code.Contains(request.Search)
                || (r.Description != null && r.Description.Contains(request.Search)));

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AdminReportItem(
                r.Id, r.Code, r.Category.Code, r.Category.NameVi,
                r.Severity, r.Status,
                r.Latitude, r.Longitude, r.Address,
                r.WardCode, r.ProvinceCode,
                r.ReporterId, r.IsAnonymous,
                r.AssignedOfficerId, r.Assignments.Count,
                r.PriorityScore, r.ReporterCount, r.ReopenedCount,
                r.CreatedAt, r.VerifiedAt, r.ResolvedAt, r.ClosedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        return new GetAdminReportsResponse(items, totalCount, request.Page, request.PageSize);
    }
}
