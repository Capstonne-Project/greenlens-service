using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.GetAdminReports;

public sealed class GetAdminReportsQueryHandler(
    IReportRepository reports,
    ILogger<GetAdminReportsQueryHandler> logger)
    : IRequestHandler<GetAdminReportsQuery, Result<GetAdminReportsResponse>>
{
    public async Task<Result<GetAdminReportsResponse>> Handle(
        GetAdminReportsQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting admin reports");

        var query = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Assignments)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            logger.LogInformation("Status: {Status}", request.Status.Value);
            query = query.Where(r => r.Status == request.Status.Value);
        }
        if (request.CategoryId.HasValue)
        {
            logger.LogInformation("CategoryId: {CategoryId}", request.CategoryId.Value);
            query = query.Where(r => r.CategoryId == request.CategoryId.Value);
        }
        if (!string.IsNullOrEmpty(request.WardCode))
        {
            logger.LogInformation("WardCode: {WardCode}", request.WardCode);
            query = query.Where(r => r.WardCode == request.WardCode);
        }
        if (!string.IsNullOrEmpty(request.ProvinceCode))
        {
            logger.LogInformation("ProvinceCode: {ProvinceCode}", request.ProvinceCode);
            query = query.Where(r => r.ProvinceCode == request.ProvinceCode);
        }
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(r => r.Code.Contains(request.Search)
                || (r.Description != null && r.Description.Contains(request.Search)));
            logger.LogInformation("Search: {Search}", request.Search);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Total count: {TotalCount}", totalCount);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AdminReportItem(
                r.Id, r.Code, r.Category.Code, r.Category.NameVi,
                r.Severity, r.Status,
                r.Latitude, r.Longitude, r.Address,
                r.WardCode, r.ProvinceCode,
                r.ReporterId,
                r.VerifiedBy, r.AssignedByOfficerId, r.Assignments.Count,
                r.PriorityScore, r.ReporterCount, r.ReopenedCount,
                r.CreatedAt, r.VerifiedAt, r.ResolvedAt, r.ClosedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Lấy danh sách báo cáo thành công. Số lượng: {Count}", items.Count);
        return new GetAdminReportsResponse(items, pagination);
    }
}
