using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetReports;

public sealed class GetReportsQueryHandler(
    IReportRepository reports,
    ILogger<GetReportsQueryHandler> logger)
    : IRequestHandler<GetReportsQuery, Result<GetReportsResponse>>
{
    public async Task<Result<GetReportsResponse>> Handle(
        GetReportsQuery request, CancellationToken ct)
    {
        
        var query = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Where(r => !r.IsHidden) // BR-ADM-006: hide moderated reports from public
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);
        if (request.CategoryId.HasValue)
            query = query.Where(r => r.CategoryId == request.CategoryId.Value);
        if (!string.IsNullOrEmpty(request.WardCode))
            query = query.Where(r => r.WardCode == request.WardCode);
        if (request.Severity.HasValue)
            query = query.Where(r => r.Severity == request.Severity.Value);

        // Tìm theo mã / mô tả / địa chỉ — ToLower() để Postgres so sánh không phân biệt hoa thường.
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLower();
            query = query.Where(r =>
                r.Code.ToLower().Contains(keyword)
                || (r.Description != null && r.Description.ToLower().Contains(keyword))
                || (r.Address != null && r.Address.ToLower().Contains(keyword)));
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

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

        logger.LogInformation("Lấy danh sách báo cáo thành công. Số lượng: {Count}", items.Count);
        return new GetReportsResponse(items, pagination);
    }
}
