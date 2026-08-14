using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetOfficeReports;

/// <summary>
/// Returns all reports scoped to the current LEO's LocalOffice,
/// including team assignment progress for each report.
/// </summary>
public sealed class GetOfficeReportsQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetOfficeReportsQueryHandler> logger)
    : IRequestHandler<GetOfficeReportsQuery, Result<GetOfficeReportsResponse>>
{
    public async Task<Result<GetOfficeReportsResponse>> Handle(
        GetOfficeReportsQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting office reports for user {UserId}", currentUser.UserId);

        // 1. Resolve LEO's local office
        var officeInfo = await users.QueryAsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new
            {
                LocalOfficeId = u.LocalOfficeId ?? Guid.Empty,
                LocalOfficeName = u.LocalOffice != null ? u.LocalOffice.Name : "",
                WardCode = u.LocalOffice != null ? u.LocalOffice.WardCode : null,
                WardName = u.LocalOffice != null && u.LocalOffice.Ward != null
                    ? u.LocalOffice.Ward.Name : null
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (officeInfo is null || officeInfo.LocalOfficeId == Guid.Empty)
        {
            logger.LogWarning("Office not found for user {UserId}", currentUser.UserId);
            return Errors.Organization.OfficeNotFound;
        }

        // 2. Base query — all reports assigned to this office
        var baseQuery = reports.QueryAsNoTracking()
            .Where(r => r.AssignedOfficeId == officeInfo.LocalOfficeId);

        // 3. Apply search (code, description, address)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            logger.LogInformation("Searching by report code, description, or address: {Search}", request.Search);
            var keyword = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(r =>
                r.Code.ToLower().Contains(keyword) ||
                (r.Description != null && r.Description.ToLower().Contains(keyword)) ||
                (r.Address != null && r.Address.ToLower().Contains(keyword)));
        }

        // 4. Apply filters
        if (request.Statuses is { Count: > 0 })
            baseQuery = baseQuery.Where(r => request.Statuses.Contains(r.Status));
        if (request.CategoryId.HasValue)
            baseQuery = baseQuery.Where(r => r.CategoryId == request.CategoryId.Value);
        if (request.Severity.HasValue)
            baseQuery = baseQuery.Where(r => r.Severity == request.Severity.Value);

        // Filter by assignment status (if any assignment matches)
        if (request.AssignmentStatus.HasValue)
        {
            baseQuery = baseQuery.Where(r =>
                r.Assignments.Any(a => a.Status == request.AssignmentStatus.Value));
        }

        if (request.FromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(request.FromDate.Value.Date, DateTimeKind.Utc);
            baseQuery = baseQuery.Where(r => r.CreatedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(request.ToDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            baseQuery = baseQuery.Where(r => r.CreatedAt < toExclusive);
        }

        // 5. Count total
        var totalItems = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalItems);

        // 6. Apply sorting
        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        var orderedQuery = sortBy switch
        {
            "code" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.Code)
                : baseQuery.OrderBy(r => r.Code),
            "status" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.Status)
                : baseQuery.OrderBy(r => r.Status),
            "severity" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.Severity)
                : baseQuery.OrderBy(r => r.Severity),
            "priority" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.PriorityScore)
                : baseQuery.OrderBy(r => r.PriorityScore),
            "createdat" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.CreatedAt)
                : baseQuery.OrderBy(r => r.CreatedAt),
            "assignmentcount" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.Assignments.Count)
                : baseQuery.OrderBy(r => r.Assignments.Count),
            _ => baseQuery.OrderByDescending(r => r.CreatedAt) // default: newest first
        };

        // 7. Paginate & project (include team assignment progress)
        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new OfficeReportItem(
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
                r.ReporterId,
                r.Reporter != null ? r.Reporter.FullName : null,
                r.Description,
                r.Assignments.Count,
                r.PriorityScore,
                r.ReporterCount,
                r.ReopenedCount,
                // Overall progress = average of non-declined assignments (Completed = 100%)
                r.Assignments.Any(a => a.Status != AssignmentStatus.Declined)
                    ? (int)r.Assignments
                        .Where(a => a.Status != AssignmentStatus.Declined)
                        .Average(a => a.Status == AssignmentStatus.Completed ? 100 : a.ProgressPercent)
                    : 0,
                r.CreatedAt,
                r.VerifiedAt,
                r.StartedAt,
                r.ResolvedAt,
                r.ClosedAt,
                r.SlaResolveDueAt,
                r.Media
                    .Where(m => m.Type == MediaType.Image)
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .Take(1)
                    .ToList(),
                r.Assignments
                    .OrderBy(a => a.AssignedAt)
                    .Select(a => new AssignmentProgressItem(
                        a.Id,
                        a.TeamId,
                        a.Team != null ? a.Team.Name : "",
                        a.Team != null ? a.Team.TeamType.ToString() : "",
                        a.Status,
                        a.Status == AssignmentStatus.Completed ? 100 : a.ProgressPercent,
                        a.ProgressNote,
                        a.Note,
                        a.DeclineReason,
                        a.AssignedAt,
                        a.StartedAt,
                        a.CompletedAt,
                        a.ProgressUpdatedAt))
                    .ToList()))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "LEO {UserId} fetched {Count} reports for office {OfficeId} (page {Page})",
            currentUser.UserId, items.Count, officeInfo.LocalOfficeId, request.Page);

        return new GetOfficeReportsResponse(
            officeInfo.LocalOfficeId,
            officeInfo.LocalOfficeName,
            officeInfo.WardCode,
            officeInfo.WardName,
            items,
            pagination);
    }
}
