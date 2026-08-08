using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetDuplicateCandidates;

/// <summary>
/// Returns paginated reports flagged as possible duplicates for LEO review.
/// </summary>
/// <remarks>
/// Implements: BR-REP-031 (possible duplicate queue), BR-REP-032 (merge review).
/// Scope: LEO → assigned office; DEO → department; Admin → all.
/// </remarks>
public sealed class GetDuplicateCandidatesQueryHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetDuplicateCandidatesQueryHandler> logger)
    : IRequestHandler<GetDuplicateCandidatesQuery, Result<GetDuplicateCandidatesResponse>>
{
    public async Task<Result<GetDuplicateCandidatesResponse>> Handle(
        GetDuplicateCandidatesQuery request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("User not found for duplicate candidates: {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        var query = reports.QueryAsNoTracking()
            .Where(r => r.IsPossibleDuplicate)
            .Where(r => r.Status != ReportStatus.Duplicate && r.Status != ReportStatus.Rejected);

        query = ReportReviewCandidateFilters.ApplyOfficerScope(query, user, currentUser.Role);

        query = ReportReviewCandidateFilters.ApplyCommon(
            query,
            request.Status,
            request.Severity,
            request.CategoryId,
            request.WardCode,
            request.FromDate,
            request.ToDate,
            request.Search);

        if (!string.IsNullOrWhiteSpace(request.DuplicateDetectionSource))
        {
            var source = request.DuplicateDetectionSource.Trim();
            query = query.Where(r => r.DuplicateDetectionSource == source);
        }

        if (request.MinAiSimilarityScore.HasValue)
        {
            var minScore = request.MinAiSimilarityScore.Value;
            query = query.Where(r => r.AiSimilarityScore >= minScore);
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var orderedQuery = ApplySort(query, request.SortBy, request.SortDir);

        var rows = await orderedQuery
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

        var firstMediaByReportId = await CitizenReportMediaLoader
            .LoadFirstByReportIdsAsync(reportMedia, reportIds, ct)
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
                    CitizenReportMediaLoader.GetFirstMediaList(firstMediaByReportId, p.Id));
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
                CitizenReportMediaLoader.GetFirstMediaList(firstMediaByReportId, r.Id),
                primary);
        }).ToList();

        logger.LogInformation("Lấy danh sách nghi ngờ trùng lặp. Số lượng: {Count}", items.Count);

        return new GetDuplicateCandidatesResponse(items, pagination);
    }

    private static IOrderedQueryable<Report> ApplySort(
        IQueryable<Report> query,
        DuplicateCandidateSortBy sortBy,
        SortDirection sortDir) =>
        (sortBy, sortDir) switch
        {
            (DuplicateCandidateSortBy.Severity, SortDirection.Asc) =>
                query.OrderBy(r => r.Severity).ThenByDescending(r => r.CreatedAt),
            (DuplicateCandidateSortBy.Severity, SortDirection.Desc) =>
                query.OrderByDescending(r => r.Severity).ThenByDescending(r => r.CreatedAt),
            (DuplicateCandidateSortBy.AiSimilarityScore, SortDirection.Asc) =>
                query.OrderBy(r => r.AiSimilarityScore ?? 0m).ThenByDescending(r => r.CreatedAt),
            (DuplicateCandidateSortBy.AiSimilarityScore, SortDirection.Desc) =>
                query.OrderByDescending(r => r.AiSimilarityScore ?? 0m).ThenByDescending(r => r.CreatedAt),
            (DuplicateCandidateSortBy.PriorityScore, SortDirection.Asc) =>
                query.OrderBy(r => r.PriorityScore).ThenByDescending(r => r.CreatedAt),
            (DuplicateCandidateSortBy.PriorityScore, SortDirection.Desc) =>
                query.OrderByDescending(r => r.PriorityScore).ThenByDescending(r => r.CreatedAt),
            (DuplicateCandidateSortBy.CreatedAt, SortDirection.Asc) =>
                query.OrderBy(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt),
        };
}
