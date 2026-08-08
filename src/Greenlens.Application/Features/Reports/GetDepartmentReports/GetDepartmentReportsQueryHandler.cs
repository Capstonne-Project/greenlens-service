using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetDepartmentReports;

/// <summary>
/// Returns all reports scoped to the current DEO's department.
/// </summary>
/// <remarks>
/// Implements: BR-OFF-010 (queue filters/sort), BR-REP-030 (duplicate flag filter),
/// BR-REP-034 (violation recurrence filter).
/// </remarks>
public sealed class GetDepartmentReportsQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetDepartmentReportsQueryHandler> logger)
    : IRequestHandler<GetDepartmentReportsQuery, Result<GetDepartmentReportsResponse>>
{
    public async Task<Result<GetDepartmentReportsResponse>> Handle(
        GetDepartmentReportsQuery request,
        CancellationToken ct)
    {
        var deptInfo = await users.QueryAsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => new
            {
                DepartmentId = u.DepartmentId ?? Guid.Empty,
                DepartmentName = u.Department != null ? u.Department.Name : ""
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (deptInfo is null || deptInfo.DepartmentId == Guid.Empty)
        {
            logger.LogWarning("Department not found for user {UserId}", currentUser.UserId);
            return Errors.Organization.DepartmentNotFound;
        }

        var baseQuery = reports.QueryAsNoTracking()
            .Where(r => r.AssignedDepartmentId == deptInfo.DepartmentId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(r =>
                r.Code.ToLower().Contains(keyword) ||
                (r.Description != null && r.Description.ToLower().Contains(keyword)) ||
                (r.Address != null && r.Address.ToLower().Contains(keyword)) ||
                r.Category.NameVi.ToLower().Contains(keyword) ||
                r.Category.Code.ToLower().Contains(keyword));
        }

        if (request.Status.HasValue)
            baseQuery = baseQuery.Where(r => r.Status == request.Status.Value);
        if (request.CategoryId.HasValue)
            baseQuery = baseQuery.Where(r => r.CategoryId == request.CategoryId.Value);
        if (request.Severity.HasValue)
            baseQuery = baseQuery.Where(r => r.Severity == request.Severity.Value);
        if (!string.IsNullOrWhiteSpace(request.WardCode))
            baseQuery = baseQuery.Where(r => r.WardCode == request.WardCode);
        if (request.AssignedOfficeId.HasValue)
            baseQuery = baseQuery.Where(r => r.AssignedOfficeId == request.AssignedOfficeId.Value);

        if (request.FromDate.HasValue)
            baseQuery = baseQuery.Where(r => r.CreatedAt >= DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc));
        if (request.ToDate.HasValue)
            baseQuery = baseQuery.Where(r => r.CreatedAt <= DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc));

        if (request.SlaBreached == true)
        {
            var now = DateTime.UtcNow;
            baseQuery = baseQuery.Where(r =>
                (r.SlaVerifyDueAt.HasValue && r.SlaVerifyDueAt < now) ||
                (r.SlaResolveDueAt.HasValue && r.SlaResolveDueAt < now));
        }

        if (request.HasPendingReopenRequest == true)
            baseQuery = baseQuery.Where(r => r.HasPendingReopenRequest);

        if (request.IsPossibleDuplicate.HasValue)
            baseQuery = baseQuery.Where(r => r.IsPossibleDuplicate == request.IsPossibleDuplicate.Value);

        if (request.IsSuspectedViolationRecurrence.HasValue)
            baseQuery = baseQuery.Where(r => r.IsSuspectedViolationRecurrence == request.IsSuspectedViolationRecurrence.Value);

        var totalItems = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalItems);

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
            "verifiedat" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.VerifiedAt)
                : baseQuery.OrderBy(r => r.VerifiedAt),
            "slaverifydueat" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.SlaVerifyDueAt)
                : baseQuery.OrderBy(r => r.SlaVerifyDueAt),
            "slaresolvedueat" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.SlaResolveDueAt)
                : baseQuery.OrderBy(r => r.SlaResolveDueAt),
            _ => baseQuery.OrderByDescending(r => r.CreatedAt)
        };

        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new DepartmentReportItem(
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
                r.AssignedOffice != null && r.AssignedOffice.Ward != null
                    ? r.AssignedOffice.Ward.Name : null,
                r.ReporterId,
                r.Reporter != null ? r.Reporter.FullName : null,
                r.AssignedOfficeId,
                r.AssignedOffice != null ? r.AssignedOffice.Name : null,
                r.Assignments.Count,
                r.PriorityScore,
                r.ReporterCount,
                r.ReopenedCount,
                r.CreatedAt,
                r.VerifiedAt,
                r.StartedAt,
                r.ResolvedAt,
                r.ClosedAt,
                r.SlaVerifyDueAt,
                r.SlaResolveDueAt,
                r.Media
                    .Where(m => m.Type == MediaType.Image)
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .FirstOrDefault(),
                r.IsPossibleDuplicate,
                r.PossibleDuplicateOfReportId,
                r.PossibleDuplicateOfReportId.HasValue
                    ? reports.QueryAsNoTracking()
                        .Where(p => p.Id == r.PossibleDuplicateOfReportId!.Value)
                        .Select(p => p.Code)
                        .FirstOrDefault()
                    : null,
                r.IsSuspectedViolationRecurrence,
                r.SuspectedRecurrenceOfReportId,
                r.SuspectedRecurrenceOfReportId.HasValue
                    ? reports.QueryAsNoTracking()
                        .Where(p => p.Id == r.SuspectedRecurrenceOfReportId!.Value)
                        .Select(p => p.Code)
                        .FirstOrDefault()
                    : null))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "DEO {UserId} fetched {Count} reports for department {DeptId} (page {Page})",
            currentUser.UserId, items.Count, deptInfo.DepartmentId, request.Page);

        return new GetDepartmentReportsResponse(
            deptInfo.DepartmentId,
            deptInfo.DepartmentName,
            items,
            pagination);
    }
}
