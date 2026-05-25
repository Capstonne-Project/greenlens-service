using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Reports.GetReports;

public sealed class GetReportsQueryHandler(
    IReportRepository reports)
    : IRequestHandler<GetReportsQuery, Result<GetReportsResponse>>
{
    public async Task<Result<GetReportsResponse>> Handle(
        GetReportsQuery request, CancellationToken ct)
    {
        var query = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);
        if (request.CategoryId.HasValue)
            query = query.Where(r => r.CategoryId == request.CategoryId.Value);
        if (!string.IsNullOrEmpty(request.WardCode))
            query = query.Where(r => r.WardCode == request.WardCode);
        if (request.Severity.HasValue)
            query = query.Where(r => r.Severity == request.Severity.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReportListItem(
                r.Id, r.Code, r.Category.Code, r.Category.NameVi,
                r.Severity, r.Status, r.Latitude, r.Longitude,
                r.Address, r.WardCode, r.ReporterCount,
                r.CreatedAt, r.ResolvedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        return new GetReportsResponse(items, totalCount, request.Page, request.PageSize);
    }
}
