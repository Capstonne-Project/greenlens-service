using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetOfficerQueue;

/// <summary>
/// Returns paginated queue of reports for the current officer's area.
/// DEO sees reports in their department that have no LocalOffice assigned (fallback queue — needs manual routing).
/// LEO sees Submitted + Verified reports for their office (needs verification or team assignment).
/// Supports search, filter, and sort (BR-OFF-010).
/// </summary>
public sealed class GetOfficerQueueQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetOfficerQueueQueryHandler> logger) : IRequestHandler<GetOfficerQueueQuery, Result<GetOfficerQueueResponse>>
{
    public async Task<Result<GetOfficerQueueResponse>> Handle(
        GetOfficerQueueQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting officer queue for user {UserId}", currentUser.UserId);

        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        var query = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .AsQueryable();

        // ── Role-based scope filtering ──
        if (user.Role == UserRole.DEO && user.DepartmentId.HasValue)
        {
            logger.LogInformation("DEO sees reports that fell into department queue (no LocalOffice assigned)");
            // DEO sees reports that fell into department queue (no LocalOffice assigned)
            query = query.Where(r =>
                r.AssignedDepartmentId == user.DepartmentId.Value &&
                r.AssignedOfficeId == null);
        }
        else if (user.Role == UserRole.LEO && user.LocalOfficeId.HasValue)
        {
            logger.LogInformation("LEO sees Submitted (needs verify) + Verified (needs team assignment) in their office");
            // LEO sees Submitted (needs verify) + Verified/Reopened (needs team assignment) in their office
            query = query.Where(r =>
                r.AssignedOfficeId == user.LocalOfficeId.Value &&
                (r.Status == ReportStatus.Submitted
                 || r.Status == ReportStatus.Verified
                 || r.Status == ReportStatus.Reopened));
        }

        // ── Filters ──
        if (request.StatusFilter.HasValue)
            query = query.Where(r => r.Status == request.StatusFilter.Value);

        if (request.SeverityFilter.HasValue)
            query = query.Where(r => r.Severity == request.SeverityFilter.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(r => r.CategoryId == request.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(request.WardCode))
            query = query.Where(r => r.WardCode == request.WardCode);

        if (request.FromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc));

        if (request.ToDate.HasValue)
            query = query.Where(r => r.CreatedAt <= DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc));

        if (request.SlaBreached == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(r =>
                (r.SlaVerifyDueAt.HasValue && r.SlaVerifyDueAt < now) ||
                (r.SlaResolveDueAt.HasValue && r.SlaResolveDueAt < now));
        }

        if (request.HasPendingReopenRequest == true)
            query = query.Where(r => r.HasPendingReopenRequest);

        if (request.IsPossibleDuplicate.HasValue)
            query = query.Where(r => r.IsPossibleDuplicate == request.IsPossibleDuplicate.Value);

        // ── Search (keyword on Code, Address, Category name) ──
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLower();
            query = query.Where(r =>
                r.Code.ToLower().Contains(keyword) ||
                (r.Address != null && r.Address.ToLower().Contains(keyword)) ||
                r.Category.NameVi.ToLower().Contains(keyword) ||
                r.Category.Code.ToLower().Contains(keyword));
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        // ── Sort ──
        var orderedQuery = ApplySort(query, request.SortBy, request.SortDir);

        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new OfficerQueueItem(
                r.Id,
                r.Code,
                r.Category.Code,
                r.Category.NameVi,
                r.Severity,
                r.Status,
                r.Latitude,
                r.Longitude,
                r.Address,
                r.WardCode,
                r.PriorityScore,
                r.CreatedAt,
                r.SlaVerifyDueAt,
                r.SlaResolveDueAt,
                r.Media
                    .Where(m => m.Type == MediaType.Image)
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .FirstOrDefault(),
                // Duplicate metadata
                r.IsPossibleDuplicate,
                r.PossibleDuplicateOfReportId,
                r.PossibleDuplicateOfReportId.HasValue
                    ? reports.QueryAsNoTracking()
                        .Where(p => p.Id == r.PossibleDuplicateOfReportId!.Value)
                        .Select(p => p.Code)
                        .FirstOrDefault()
                    : null,
                r.DuplicateDetectionSource,
                r.AiSimilarityScore,
                reports.QueryAsNoTracking()
                    .Count(d => d.IsPossibleDuplicate && d.PossibleDuplicateOfReportId == r.Id)))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Lấy danh sách báo cáo thành công. Số lượng: {Count}", items.Count);
        return new GetOfficerQueueResponse(items, pagination);
    }

    private static IOrderedQueryable<Domain.Entities.Report> ApplySort(
        IQueryable<Domain.Entities.Report> query,
        QueueSortBy sortBy,
        SortDirection sortDir)
    {
        return (sortBy, sortDir) switch
        {
            (QueueSortBy.PriorityScore, SortDirection.Asc) => query.OrderBy(r => r.PriorityScore).ThenBy(r => r.CreatedAt),
            (QueueSortBy.PriorityScore, SortDirection.Desc) => query.OrderByDescending(r => r.PriorityScore).ThenByDescending(r => r.CreatedAt),
            (QueueSortBy.CreatedAt, SortDirection.Asc) => query.OrderBy(r => r.CreatedAt),
            (QueueSortBy.CreatedAt, SortDirection.Desc) => query.OrderByDescending(r => r.CreatedAt),
            (QueueSortBy.Severity, SortDirection.Asc) => query.OrderBy(r => r.Severity).ThenByDescending(r => r.CreatedAt),
            (QueueSortBy.Severity, SortDirection.Desc) => query.OrderByDescending(r => r.Severity).ThenByDescending(r => r.CreatedAt),
            (QueueSortBy.SlaVerifyDueAt, SortDirection.Asc) => query.OrderBy(r => r.SlaVerifyDueAt).ThenByDescending(r => r.CreatedAt),
            (QueueSortBy.SlaVerifyDueAt, SortDirection.Desc) => query.OrderByDescending(r => r.SlaVerifyDueAt).ThenByDescending(r => r.CreatedAt),
            (QueueSortBy.SlaResolveDueAt, SortDirection.Asc) => query.OrderBy(r => r.SlaResolveDueAt).ThenByDescending(r => r.CreatedAt),
            (QueueSortBy.SlaResolveDueAt, SortDirection.Desc) => query.OrderByDescending(r => r.SlaResolveDueAt).ThenByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.PriorityScore).ThenByDescending(r => r.CreatedAt),
        };
    }
}
