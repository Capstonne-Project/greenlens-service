using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetDuplicateCandidates;

/// <summary>
/// Returns paginated reports flagged as possible duplicates for LEO review.
/// </summary>
/// <remarks>Implements: BR-REP-031 (possible duplicate queue), BR-REP-032 (merge review).</remarks>
public sealed class GetDuplicateCandidatesQueryHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    ILogger<GetDuplicateCandidatesQueryHandler> logger)
    : IRequestHandler<GetDuplicateCandidatesQuery, Result<GetDuplicateCandidatesResponse>>
{
    public async Task<Result<GetDuplicateCandidatesResponse>> Handle(
        GetDuplicateCandidatesQuery request, CancellationToken ct)
    {
        var query = reports.QueryAsNoTracking()
            .Where(r => r.IsPossibleDuplicate)
            .Where(r => r.Status != ReportStatus.Duplicate && r.Status != ReportStatus.Rejected);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var rows = await query
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
                r.Latitude,
                r.Longitude,
                r.Address,
                r.CreatedAt,
                r.DuplicateDetectionSource,
                r.AiSimilarityScore,
                r.PossibleDuplicateOfReportId
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var reportIds = rows
            .Select(r => r.Id)
            .Concat(rows.Where(r => r.PossibleDuplicateOfReportId.HasValue).Select(r => r.PossibleDuplicateOfReportId!.Value))
            .Distinct()
            .ToList();

        var mediaByReportId = await CitizenReportMediaLoader.LoadByReportIdsAsync(reportMedia, reportIds, ct)
            .ConfigureAwait(false);

        var primaryIds = rows
            .Where(r => r.PossibleDuplicateOfReportId.HasValue)
            .Select(r => r.PossibleDuplicateOfReportId!.Value)
            .Distinct()
            .ToList();

        var primaryMeta = primaryIds.Count == 0
            ? []
            : await reports.QueryAsNoTracking()
                .Where(p => primaryIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Code, p.Address, p.CreatedAt })
                .ToListAsync(ct)
                .ConfigureAwait(false);

        var primaryById = primaryMeta.ToDictionary(p => p.Id);

        var items = rows.Select(r =>
        {
            DuplicateCandidatePrimary? primary = null;
            if (r.PossibleDuplicateOfReportId is { } pid && primaryById.TryGetValue(pid, out var p))
            {
                primary = new DuplicateCandidatePrimary(
                    p.Id,
                    p.Code,
                    p.Address,
                    p.CreatedAt,
                    CitizenReportMediaLoader.GetMediaOrEmpty(mediaByReportId, p.Id));
            }

            return new DuplicateCandidateItem(
                r.Id,
                r.Code,
                r.CategoryName,
                r.Severity,
                r.Status,
                r.Latitude,
                r.Longitude,
                r.Address,
                r.CreatedAt,
                r.DuplicateDetectionSource,
                r.AiSimilarityScore,
                CitizenReportMediaLoader.GetMediaOrEmpty(mediaByReportId, r.Id),
                primary);
        }).ToList();

        logger.LogInformation("Lấy danh sách nghi ngờ trùng lặp. Số lượng: {Count}", items.Count);

        return new GetDuplicateCandidatesResponse(items, pagination);
    }
}
