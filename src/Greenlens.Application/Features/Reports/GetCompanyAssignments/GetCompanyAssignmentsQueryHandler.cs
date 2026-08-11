using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetCompanyAssignments;

/// <summary>
/// Returns all assignments for the caller's company, including team info and progress.
/// CM can see the full picture: which team → which report → status + progress.
/// </summary>
/// <remarks>Implements: BR-CMP-021.</remarks>
public sealed class GetCompanyAssignmentsQueryHandler(
    IReportAssignmentRepository assignments,
    IReportMediaRepository reportMedia,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    ILogger<GetCompanyAssignmentsQueryHandler> logger)
    : IRequestHandler<GetCompanyAssignmentsQuery, Result<GetCompanyAssignmentsResponse>>
{
    public async Task<Result<GetCompanyAssignmentsResponse>> Handle(
        GetCompanyAssignmentsQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting company assignments for user {UserId}", currentUser.UserId);

        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;

        logger.LogInformation("Company ID: {CompanyId}", companyId);

        // ── 2. Build query: all assignments where team belongs to this company ──
        var baseQuery = assignments.QueryAsNoTracking()
            .Include(a => a.Report).ThenInclude(r => r!.Category)
            .Include(a => a.Team).ThenInclude(t => t!.Members)
            .Include(a => a.AssignedByUser)
            .Where(a => a.Team!.CompanyId == companyId);

        // Filter by assignment status
        if (request.Status.HasValue)
        {
            logger.LogInformation("Filtering by assignment status: {Status}", request.Status.Value);
            baseQuery = baseQuery.Where(a => a.Status == request.Status.Value);
        }

        // Filter by report status
        if (request.ReportStatus.HasValue)
        {
            logger.LogInformation("Filtering by report status: {Status}", request.ReportStatus.Value);
            baseQuery = baseQuery.Where(a => a.Report!.Status == request.ReportStatus.Value);
        }

        // Search by report code or address
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            logger.LogInformation("Searching by report code or address: {Search}", request.Search);
            var search = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(a =>
                a.Report!.Code.ToLower().Contains(search) ||
                (a.Report.Address != null && a.Report.Address.ToLower().Contains(search)) ||
                a.Team!.Name.ToLower().Contains(search));
        }

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var rows = await baseQuery
            .OrderByDescending(a => a.AssignedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AssignmentRow(
                a.Id,
                a.Status,
                a.AssignedAt,
                a.StartedAt,
                a.CompletedAt,
                a.ProgressPercent,
                a.ProgressNote,
                a.ProgressUpdatedAt,
                a.Note,
                a.Report!.Id,
                a.Report.Code,
                a.Report.Address,
                a.Report.WardCode,
                a.Report.Category.NameVi,
                a.Report.Severity,
                a.Report.Status,
                a.Report.SlaResolveDueAt,
                a.Team!.Id,
                a.Team.Name,
                a.Team.Members.Count,
                a.AssignedByUser != null ? a.AssignedByUser.FullName : "Unknown"))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var reportIds = rows.Select(r => r.ReportId).Distinct().ToList();
        var firstMediaByReportId = await CitizenReportMediaLoader
            .LoadFirstByReportIdsAsync(reportMedia, reportIds, ct)
            .ConfigureAwait(false);

        var items = rows.Select(r => new CompanyAssignmentItem(
            r.AssignmentId,
            r.AssignmentStatus,
            r.AssignedAt,
            r.StartedAt,
            r.CompletedAt,
            r.ProgressPercent,
            r.ProgressNote,
            r.ProgressUpdatedAt,
            r.Note,
            new CompanyAssignmentReport(
                r.ReportId,
                r.ReportCode,
                r.ReportAddress,
                r.WardCode,
                r.CategoryName,
                r.Severity,
                r.ReportStatus,
                r.SlaResolveDueAt,
                CitizenReportMediaLoader.GetFirstMediaList(firstMediaByReportId, r.ReportId)),
            new CompanyAssignmentTeam(
                r.TeamId,
                r.TeamName,
                r.MemberCount),
            r.AssignedByName)).ToList();

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, total);

        logger.LogInformation(
            "CM {UserId} viewed company assignments: {Count}/{Total} for company {CompanyId}",
            currentUser.UserId, items.Count, total, companyId);

        return new GetCompanyAssignmentsResponse(items, pagination);
    }

    private sealed record AssignmentRow(
        Guid AssignmentId,
        AssignmentStatus AssignmentStatus,
        DateTime AssignedAt,
        DateTime? StartedAt,
        DateTime? CompletedAt,
        int ProgressPercent,
        string? ProgressNote,
        DateTime? ProgressUpdatedAt,
        string? Note,
        Guid ReportId,
        string ReportCode,
        string? ReportAddress,
        string? WardCode,
        string CategoryName,
        Severity Severity,
        ReportStatus ReportStatus,
        DateTime? SlaResolveDueAt,
        Guid TeamId,
        string TeamName,
        int MemberCount,
        string AssignedByName);
}
