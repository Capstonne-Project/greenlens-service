using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetOfficeReports;

/// <summary>
/// Returns all reports scoped to the current LEO's LocalOffice,
/// including current-cycle team assignment progress for each report.
/// </summary>
/// <remarks>Implements: BR-REP-015 (reopen creates new assignment cycle), BR-CMP-005 (company dispatch), BR-CMU-003 (optional community-cleanup filter).</remarks>
public sealed class GetOfficeReportsQueryHandler(
    IReportRepository reports,
    ICommunityCleanupEventRepository communityCleanupEvents,
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

        var baseQuery = reports.QueryAsNoTracking()
            .Where(r => r.AssignedOfficeId == officeInfo.LocalOfficeId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            logger.LogInformation("Searching by report code, description, or address: {Search}", request.Search);
            var keyword = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(r =>
                r.Code.ToLower().Contains(keyword) ||
                (r.Description != null && r.Description.ToLower().Contains(keyword)) ||
                (r.Address != null && r.Address.ToLower().Contains(keyword)));
        }

        if (request.Statuses is { Count: > 0 })
            baseQuery = baseQuery.Where(r => request.Statuses.Contains(r.Status));
        if (request.CategoryId.HasValue)
            baseQuery = baseQuery.Where(r => r.CategoryId == request.CategoryId.Value);
        if (request.Severity.HasValue)
            baseQuery = baseQuery.Where(r => r.Severity == request.Severity.Value);

        if (request.AssignmentStatus.HasValue)
        {
            var statusFilter = request.AssignmentStatus.Value;
            baseQuery = baseQuery.Where(r =>
                // Active open assignment (current cycle)
                r.Assignments.Any(a =>
                    (a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress)
                    && a.Status == statusFilter
                    && !r.Assignments.Any(a2 =>
                        (a2.Status == AssignmentStatus.Assigned || a2.Status == AssignmentStatus.InProgress)
                        && a2.AssignedAt > a.AssignedAt))
                ||
                // Display cycle when no open assignment and not awaiting re-assign (BR-REP-015)
                (r.Status != ReportStatus.Reopened
                 && r.Status != ReportStatus.InProgress
                 && !r.Assignments.Any(a =>
                     a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress)
                 && r.Assignments
                     .Where(a => a.Status != AssignmentStatus.Declined)
                     .OrderByDescending(a => a.AssignedAt)
                     .Select(a => a.Status)
                     .FirstOrDefault() == statusFilter));
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

        if (request.TeamScope == OfficeReportTeamScope.Company)
        {
            baseQuery = baseQuery.Where(r =>
                r.AssignedCompanyId != null ||
                r.Assignments.Any(a =>
                    (a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) &&
                    a.Team != null &&
                    a.Team.CompanyId != null));
        }
        else if (request.TeamScope == OfficeReportTeamScope.Community)
        {
            baseQuery = baseQuery.Where(r =>
                r.AssignedCompanyId == null &&
                r.Assignments.Any(a =>
                    (a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress) &&
                    a.Team != null &&
                    a.Team.CompanyId == null));
        }

        if (request.HasActiveCommunityCleanup is true)
        {
            baseQuery = baseQuery.Where(r => communityCleanupEvents.QueryAsNoTracking()
                .Any(e => e.ReportId == r.Id
                    && e.Status != CommunityCleanupStatus.Completed
                    && e.Status != CommunityCleanupStatus.Cancelled));
        }
        else if (request.HasActiveCommunityCleanup is false)
        {
            baseQuery = baseQuery.Where(r => !communityCleanupEvents.QueryAsNoTracking()
                .Any(e => e.ReportId == r.Id
                    && e.Status != CommunityCleanupStatus.Completed
                    && e.Status != CommunityCleanupStatus.Cancelled));
        }

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
            "assignmentcount" => request.SortDesc
                ? baseQuery.OrderByDescending(r => r.Assignments.Count)
                : baseQuery.OrderBy(r => r.Assignments.Count),
            _ => baseQuery.OrderByDescending(r => r.CreatedAt)
        };

        var pageReports = await orderedQuery
            .Include(r => r.Category)
            .Include(r => r.Reporter)
            .Include(r => r.Media)
            .Include(r => r.AssignedCompany)
            .Include(r => r.Assignments)
                .ThenInclude(a => a.Team)
            .Include(r => r.Assignments)
                .ThenInclude(a => a.ProgressUpdates)
            .Include(r => r.StatusHistory)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = pageReports.Select(MapOfficeReportItem).ToList();

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

    private static OfficeReportItem MapOfficeReportItem(Report r)
    {
        var latestPerTeam = ReportAssignmentSelection.SelectLatestPerTeam(r.Assignments);
        var cycleStartAt = ReportAssignmentSelection.ResolveCycleStartAt(
            r.ReopenedCount,
            r.StatusHistory,
            latestPerTeam);

        var currentAssignment = ReportAssignmentSelection.ResolveProgressAssignment(
            r.Assignments,
            r.Status,
            r.ReopenedCount,
            r.StatusHistory);

        IReadOnlyList<AssignmentProgressItem> assignments = currentAssignment is null
            ? []
            : [MapAssignmentProgress(currentAssignment, r.Media)];

        var overallProgress = currentAssignment is null
            ? 0
            : currentAssignment.Status == AssignmentStatus.Completed
                ? 100
                : currentAssignment.ProgressPercent;

        OfficeAssignedCompanyItem? assignedCompany = null;
        if (r.AssignedCompanyId.HasValue
            && r.AssignedCompany is not null
            && ReportAssignmentSelection.IsCompanyDispatchInCurrentCycle(
                r.ReopenedCount, r.DispatchedToCompanyAt, cycleStartAt))
        {
            assignedCompany = new OfficeAssignedCompanyItem(
                r.AssignedCompanyId.Value,
                r.AssignedCompany.Name,
                r.DispatchedToCompanyAt);
        }

        var thumbnails = r.Media
            .Where(m => m.Type == MediaType.Image)
            .OrderBy(m => m.UploadedAt)
            .Select(m => m.ThumbnailUrl ?? m.Url)
            .Take(1)
            .ToList();

        return new OfficeReportItem(
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
            r.Reporter?.FullName,
            r.Description,
            r.Assignments.Count,
            r.PriorityScore,
            r.ReporterCount,
            r.ReopenedCount,
            overallProgress,
            r.CreatedAt,
            r.VerifiedAt,
            r.StartedAt,
            r.ResolvedAt,
            r.ClosedAt,
            r.SlaResolveDueAt,
            thumbnails,
            assignedCompany,
            assignments);
    }

    private static AssignmentProgressItem MapAssignmentProgress(
        ReportAssignment a,
        IEnumerable<ReportMedia> reportMedia)
    {
        var beforeUrls = ReportAssignmentMediaScope
            .FilterForAssignment(reportMedia, a, MediaType.Before)
            .Select(m => m.ThumbnailUrl ?? m.Url)
            .ToList();
        var afterUrls = ReportAssignmentMediaScope
            .FilterForAssignment(reportMedia, a, MediaType.After)
            .Select(m => m.ThumbnailUrl ?? m.Url)
            .ToList();

        return new(
            a.Id,
            a.TeamId,
            a.Team?.Name ?? string.Empty,
            a.Team?.TeamType.ToString() ?? string.Empty,
            a.Team?.IsCompanyTeam ?? false,
            a.Status,
            a.Status == AssignmentStatus.Completed ? 100 : a.ProgressPercent,
            ReportAssignmentMediaScope.ResolveLatestProgressNote(a),
            a.Note,
            a.DeclineReason,
            a.AssignedAt,
            a.StartedAt,
            a.CompletedAt,
            a.ProgressUpdatedAt,
            beforeUrls,
            afterUrls);
    }
}
