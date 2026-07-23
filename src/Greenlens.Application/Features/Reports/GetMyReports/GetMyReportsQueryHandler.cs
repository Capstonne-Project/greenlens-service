using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetMyReports;

public sealed class GetMyReportsQueryHandler(
    IReportRepository reports,
    ICurrentUser currentUser,
    ILogger<GetMyReportsQueryHandler> logger)
    : IRequestHandler<GetMyReportsQuery, Result<GetMyReportsResponse>>
{
    public async Task<Result<GetMyReportsResponse>> Handle(
        GetMyReportsQuery request, CancellationToken ct)
    {
        var query = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Media)
            .Where(r => r.ReporterId == currentUser.UserId);

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new MyReportItem(
                r.Id, r.Code, r.Category.NameVi,
                r.Severity, r.Status, r.Address,
                r.CreatedAt, r.ResolvedAt, r.ClosedAt,
                r.Media
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .FirstOrDefault(),
                r.ParentReportId,
                r.ParentReportId.HasValue
                    ? reports.QueryAsNoTracking()
                        .Where(p => p.Id == r.ParentReportId!.Value)
                        .Select(p => p.Code)
                        .FirstOrDefault()
                    : null))
            .ToListAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Lấy danh sách báo cáo thành công. Số lượng: {Count}", items.Count);
        return new GetMyReportsResponse(items, pagination);
    }
}
