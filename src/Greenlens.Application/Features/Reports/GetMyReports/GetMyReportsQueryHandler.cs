using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetMyReports;

/// <summary>
/// List the current user's reports with cover image — including Duplicate items
/// whose media was reassigned to the primary (BR-REP-032).
/// </summary>
public sealed class GetMyReportsQueryHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    ICurrentUser currentUser,
    ILogger<GetMyReportsQueryHandler> logger)
    : IRequestHandler<GetMyReportsQuery, Result<GetMyReportsResponse>>
{
    public async Task<Result<GetMyReportsResponse>> Handle(
        GetMyReportsQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting my reports for user {UserId}", currentUser.UserId);

        var query = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Media)
            .Where(r => r.ReporterId == currentUser.UserId);

        if (request.Status.HasValue)
        {
            logger.LogInformation("Filtering by report status: {Status}", request.Status.Value);
            query = query.Where(r => r.Status == request.Status.Value);
        }
        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var pageRows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new
            {
                r.Id,
                r.Code,
                CategoryName = r.Category.NameVi,
                r.Severity,
                r.Status,
                r.Address,
                r.CreatedAt,
                r.ResolvedAt,
                r.ClosedAt,
                OwnImageUrl = r.Media
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .FirstOrDefault(),
                r.ParentReportId,
                PrimaryCode = r.ParentReportId.HasValue
                    ? reports.QueryAsNoTracking()
                        .Where(p => p.Id == r.ParentReportId!.Value)
                        .Select(p => p.Code)
                        .FirstOrDefault()
                    : null
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // BR-REP-032: Duplicate rows lose own Media after reassign — project from primary by SourceReportId.
        var missingThumbIds = pageRows
            .Where(r => string.IsNullOrEmpty(r.OwnImageUrl)
                        && r.ParentReportId.HasValue
                        && r.Status == ReportStatus.Duplicate)
            .Select(r => r.Id)
            .ToList();

        Dictionary<Guid, string> sourceThumbs = [];
        if (missingThumbIds.Count > 0)
        {
            var thumbRows = await reportMedia.QueryAsNoTracking()
                .Where(m => m.SourceReportId != null && missingThumbIds.Contains(m.SourceReportId.Value))
                .OrderBy(m => m.UploadedAt)
                .Select(m => new { SourceId = m.SourceReportId!.Value, Url = m.ThumbnailUrl ?? m.Url })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            sourceThumbs = thumbRows
                .GroupBy(x => x.SourceId)
                .ToDictionary(g => g.Key, g => g.First().Url);
        }

        var items = pageRows
            .Select(r => new MyReportItem(
                r.Id, r.Code, r.CategoryName,
                r.Severity, r.Status, r.Address,
                r.CreatedAt, r.ResolvedAt, r.ClosedAt,
                !string.IsNullOrEmpty(r.OwnImageUrl)
                    ? r.OwnImageUrl
                    : sourceThumbs.GetValueOrDefault(r.Id),
                r.ParentReportId,
                r.PrimaryCode))
            .ToList();

        logger.LogInformation("Lấy danh sách báo cáo thành công. Số lượng: {Count}", items.Count);
        return new GetMyReportsResponse(items, pagination);
    }
}
