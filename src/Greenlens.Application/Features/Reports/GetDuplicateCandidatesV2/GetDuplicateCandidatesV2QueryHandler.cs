using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Application.Features.Reports.GetDuplicateCandidates;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetDuplicateCandidatesV2;

/// <summary>
/// Returns possible-duplicate reports grouped by their primary (canonical) report for LEO review.
/// </summary>
/// <remarks>
/// Implements: BR-REP-031 (possible duplicate queue), BR-REP-032 (merge review).
/// Scope: LEO → assigned office; DEO → department; Admin → all.
/// </remarks>
public sealed class GetDuplicateCandidatesV2QueryHandler(
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetDuplicateCandidatesV2QueryHandler> logger)
    : IRequestHandler<GetDuplicateCandidatesV2Query, Result<GetDuplicateCandidatesV2Response>>
{
    public async Task<Result<GetDuplicateCandidatesV2Response>> Handle(
        GetDuplicateCandidatesV2Query request,
        CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("User not found for duplicate candidates v2: {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        var duplicateQuery = BuildDuplicateQuery(reports, request, user, currentUser.Role);

        var groupStatsQuery = duplicateQuery
            .GroupBy(r => r.PossibleDuplicateOfReportId!.Value)
            .Select(g => new
            {
                PrimaryId = g.Key,
                LatestCreatedAt = g.Max(x => x.CreatedAt),
                MaxSeverity = g.Max(x => x.Severity),
                MaxAiScore = g.Max(x => x.AiSimilarityScore ?? 0m),
                MaxPriorityScore = g.Max(x => x.PriorityScore),
                DuplicateCount = g.Count()
            });

        var totalCount = await groupStatsQuery.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var orderedQuery = (request.SortBy, request.SortDir) switch
        {
            (DuplicateCandidateSortBy.Severity, SortDirection.Asc) =>
                groupStatsQuery.OrderBy(g => g.MaxSeverity).ThenByDescending(g => g.LatestCreatedAt),
            (DuplicateCandidateSortBy.Severity, SortDirection.Desc) =>
                groupStatsQuery.OrderByDescending(g => g.MaxSeverity).ThenByDescending(g => g.LatestCreatedAt),
            (DuplicateCandidateSortBy.AiSimilarityScore, SortDirection.Asc) =>
                groupStatsQuery.OrderBy(g => g.MaxAiScore).ThenByDescending(g => g.LatestCreatedAt),
            (DuplicateCandidateSortBy.AiSimilarityScore, SortDirection.Desc) =>
                groupStatsQuery.OrderByDescending(g => g.MaxAiScore).ThenByDescending(g => g.LatestCreatedAt),
            (DuplicateCandidateSortBy.PriorityScore, SortDirection.Asc) =>
                groupStatsQuery.OrderBy(g => g.MaxPriorityScore).ThenByDescending(g => g.LatestCreatedAt),
            (DuplicateCandidateSortBy.PriorityScore, SortDirection.Desc) =>
                groupStatsQuery.OrderByDescending(g => g.MaxPriorityScore).ThenByDescending(g => g.LatestCreatedAt),
            (DuplicateCandidateSortBy.CreatedAt, SortDirection.Asc) =>
                groupStatsQuery.OrderBy(g => g.LatestCreatedAt),
            _ => groupStatsQuery.OrderByDescending(g => g.LatestCreatedAt),
        };

        var pageStats = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (pageStats.Count == 0)
        {
            logger.LogInformation("Lấy danh sách nghi ngờ trùng lặp v2 (theo báo cáo gốc). Số nhóm: 0");
            return new GetDuplicateCandidatesV2Response([], pagination);
        }

        var pagePrimaryIds = pageStats.Select(s => s.PrimaryId).ToList();

        var duplicateRows = await duplicateQuery
            .Where(r => pagePrimaryIds.Contains(r.PossibleDuplicateOfReportId!.Value))
            .Select(r => new DuplicateRow(
                r.Id,
                r.Code,
                r.Category.NameVi,
                r.Severity,
                r.Status,
                r.Latitude,
                r.Longitude,
                r.Address,
                r.CreatedAt,
                r.DuplicateDetectionSource,
                r.AiSimilarityScore,
                r.PossibleDuplicateOfReportId!.Value))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var reportIds = duplicateRows
            .Select(r => r.Id)
            .Concat(pagePrimaryIds)
            .Distinct()
            .ToList();

        var firstMediaByReportId = await CitizenReportMediaLoader
            .LoadFirstByReportIdsAsync(reportMedia, reportIds, ct)
            .ConfigureAwait(false);

        var primaryMeta = await reports.QueryAsNoTracking()
            .Where(p => pagePrimaryIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Code, p.Address, p.CreatedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var primaryById = primaryMeta.ToDictionary(p => p.Id);

        var duplicatesByPrimary = duplicateRows
            .GroupBy(r => r.PrimaryReportId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.CreatedAt).ToList());

        var items = pageStats
            .Where(s => primaryById.ContainsKey(s.PrimaryId))
            .Select(stat =>
            {
                var primary = primaryById[stat.PrimaryId];
                var duplicates = duplicatesByPrimary.GetValueOrDefault(stat.PrimaryId) ?? [];

                return new DuplicateCandidateGroupItem(
                    new DuplicateCandidatePrimary(
                        primary.Id,
                        primary.Code,
                        primary.Address,
                        primary.CreatedAt,
                        CitizenReportMediaLoader.GetFirstMediaList(firstMediaByReportId, primary.Id)),
                    duplicates.Select(d => new DuplicateCandidateEntry(
                        d.Id,
                        d.Code,
                        d.CategoryName,
                        d.Severity,
                        d.Status,
                        d.Latitude,
                        d.Longitude,
                        d.Address,
                        d.CreatedAt,
                        d.DuplicateDetectionSource,
                        d.AiSimilarityScore,
                        CitizenReportMediaLoader.GetFirstMediaList(firstMediaByReportId, d.Id))).ToList(),
                    stat.DuplicateCount);
            })
            .ToList();

        logger.LogInformation(
            "Lấy danh sách nghi ngờ trùng lặp v2 (theo báo cáo gốc). Số nhóm: {Count}",
            items.Count);

        return new GetDuplicateCandidatesV2Response(items, pagination);
    }

    private static IQueryable<Report> BuildDuplicateQuery(
        IReportRepository reports,
        GetDuplicateCandidatesV2Query request,
        User user,
        string role)
    {
        var query = reports.QueryAsNoTracking()
            .Where(r => r.IsPossibleDuplicate)
            .Where(r => r.PossibleDuplicateOfReportId != null)
            .Where(r => r.Status != ReportStatus.Duplicate && r.Status != ReportStatus.Rejected);

        query = ReportReviewCandidateFilters.ApplyDuplicateReviewScope(
            query, reports.QueryAsNoTracking(), user, role);

        query = ReportReviewCandidateFilters.ApplyCommon(
            query,
            request.Status,
            request.Severity,
            request.CategoryId,
            request.WardCode,
            request.FromDate,
            request.ToDate,
            request.Search);

        if (request.PrimaryReportId.HasValue)
            query = query.Where(r => r.PossibleDuplicateOfReportId == request.PrimaryReportId.Value);

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

        return query;
    }

    private sealed record DuplicateRow(
        Guid Id,
        string Code,
        string CategoryName,
        Severity Severity,
        ReportStatus Status,
        decimal Latitude,
        decimal Longitude,
        string? Address,
        DateTime CreatedAt,
        string? DuplicateDetectionSource,
        decimal? AiSimilarityScore,
        Guid PrimaryReportId);
}
