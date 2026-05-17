using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Reports.GetMyReports;

public sealed class GetMyReportsQueryHandler(
    IReportRepository reports,
    ICurrentUser currentUser)
    : IRequestHandler<GetMyReportsQuery, Result<GetMyReportsResponse>>
{
    public async Task<Result<GetMyReportsResponse>> Handle(
        GetMyReportsQuery request, CancellationToken ct)
    {
        var query = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.ReporterId == currentUser.UserId);

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new MyReportItem(
                r.Id, r.Code, r.Category.NameVi,
                r.Severity, r.Status, r.Address,
                r.CreatedAt, r.ResolvedAt, r.ClosedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        return new GetMyReportsResponse(items, totalCount, request.Page, request.PageSize);
    }
}
