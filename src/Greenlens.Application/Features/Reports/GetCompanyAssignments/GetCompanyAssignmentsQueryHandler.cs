using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
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
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        logger.LogInformation("Company ID: {CompanyId}", companyId);

        var baseQuery = assignments.QueryAsNoTracking()
            .Include(a => a.Report).ThenInclude(r => r!.Category)
            .Include(a => a.Team).ThenInclude(t => t!.WasteTags).ThenInclude(tw => tw.WasteTag)
            .Include(a => a.Team).ThenInclude(t => t!.Members).ThenInclude(m => m.User)
            .Include(a => a.AssignedByUser)
            .Where(a => a.Team!.CompanyId == companyId);

        if (request.Status.HasValue)
        {
            logger.LogInformation("Filtering by assignment status: {Status}", request.Status.Value);
            baseQuery = baseQuery.Where(a => a.Status == request.Status.Value);
        }

        if (request.ReportStatus.HasValue)
        {
            logger.LogInformation("Filtering by report status: {Status}", request.ReportStatus.Value);
            baseQuery = baseQuery.Where(a => a.Report!.Status == request.ReportStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLowerInvariant();
            logger.LogInformation("Search: {Search}", request.Search);
            baseQuery = baseQuery.Where(a =>
                a.Report!.Code.ToLower().Contains(keyword) ||
                (a.Report.Address != null && a.Report.Address.ToLower().Contains(keyword)) ||
                (a.Report.WardCode != null && a.Report.WardCode.ToLower().Contains(keyword)) ||
                a.Report.Category.NameVi.ToLower().Contains(keyword) ||
                a.Report.Category.NameEn.ToLower().Contains(keyword) ||
                a.Team!.Name.ToLower().Contains(keyword));
        }

        if (request.Severity.HasValue)
        {
            logger.LogInformation("Filtering by severity: {Severity}", request.Severity.Value);
            baseQuery = baseQuery.Where(a => a.Report!.Severity == request.Severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.WardCode))
        {
            var ward = request.WardCode.Trim();
            logger.LogInformation("Filtering by ward: {WardCode}", ward);
            baseQuery = baseQuery.Where(a => a.Report!.WardCode == ward);
        }

        if (request.CategoryId.HasValue)
        {
            logger.LogInformation("Filtering by category: {CategoryId}", request.CategoryId.Value);
            baseQuery = baseQuery.Where(a => a.Report!.CategoryId == request.CategoryId.Value);
        }

        if (request.TeamId.HasValue)
        {
            logger.LogInformation("Filtering by team: {TeamId}", request.TeamId.Value);
            baseQuery = baseQuery.Where(a => a.TeamId == request.TeamId.Value);
        }

        if (request.FromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(request.FromDate.Value.Date, DateTimeKind.Utc);
            logger.LogInformation("From date: {FromDate}", from);
            baseQuery = baseQuery.Where(a => a.AssignedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(request.ToDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            logger.LogInformation("To date (exclusive): {ToDate}", toExclusive);
            baseQuery = baseQuery.Where(a => a.AssignedAt < toExclusive);
        }

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(page, pageSize, total);

        var sortBy = request.SortBy?.Trim().ToLowerInvariant();
        logger.LogInformation("Sort by: {SortBy}", sortBy);
        var orderedQuery = sortBy switch
        {
            "code" => request.SortDesc
                ? baseQuery.OrderByDescending(a => a.Report!.Code)
                : baseQuery.OrderBy(a => a.Report!.Code),
            "severity" => request.SortDesc
                ? baseQuery.OrderByDescending(a => a.Report!.Severity)
                : baseQuery.OrderBy(a => a.Report!.Severity),
            "reportstatus" => request.SortDesc
                ? baseQuery.OrderByDescending(a => a.Report!.Status)
                : baseQuery.OrderBy(a => a.Report!.Status),
            "status" => request.SortDesc
                ? baseQuery.OrderByDescending(a => a.Status)
                : baseQuery.OrderBy(a => a.Status),
            "progresspercent" => request.SortDesc
                ? baseQuery.OrderByDescending(a => a.ProgressPercent)
                : baseQuery.OrderBy(a => a.ProgressPercent),
            "startedat" => request.SortDesc
                ? baseQuery.OrderByDescending(a => a.StartedAt)
                : baseQuery.OrderBy(a => a.StartedAt),
            "completedat" => request.SortDesc
                ? baseQuery.OrderByDescending(a => a.CompletedAt)
                : baseQuery.OrderBy(a => a.CompletedAt),
            "slaresolvedueat" => request.SortDesc
                ? baseQuery.OrderByDescending(a => a.Report!.SlaResolveDueAt)
                : baseQuery.OrderBy(a => a.Report!.SlaResolveDueAt),
            "teamname" => request.SortDesc
                ? baseQuery.OrderByDescending(a => a.Team!.Name)
                : baseQuery.OrderBy(a => a.Team!.Name),
            "assignedat" => request.SortDesc
                ? baseQuery.OrderByDescending(a => a.AssignedAt)
                : baseQuery.OrderBy(a => a.AssignedAt),
            _ => baseQuery.OrderByDescending(a => a.AssignedAt)
        };

        var pageAssignments = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var reportIds = pageAssignments.Select(a => a.Report!.Id).Distinct().ToList();
        var firstMediaByReportId = await CitizenReportMediaLoader
            .LoadFirstByReportIdsAsync(reportMedia, reportIds, ct)
            .ConfigureAwait(false);

        var items = pageAssignments.Select(a => new CompanyAssignmentItem(
            a.Id,
            a.Status,
            a.AssignedAt,
            a.StartedAt,
            a.CompletedAt,
            a.ProgressPercent,
            a.ProgressNote,
            a.ProgressUpdatedAt,
            a.Note,
            new CompanyAssignmentReport(
                a.Report!.Id,
                a.Report.Code,
                a.Report.Address,
                a.Report.WardCode,
                a.Report.Category.NameVi,
                a.Report.Severity,
                a.Report.Status,
                a.Report.SlaResolveDueAt,
                CitizenReportMediaLoader.GetFirstMedia(firstMediaByReportId, a.Report.Id)),
            MapTeam(a.Team!),
            a.AssignedByUser?.FullName ?? "Unknown")).ToList();

        logger.LogInformation(
            "CM {UserId} viewed company assignments: {Count}/{Total} for company {CompanyId}",
            currentUser.UserId, items.Count, total, companyId);

        return new GetCompanyAssignmentsResponse(items, pagination);
    }

    private static CompanyAssignmentTeam MapTeam(EnvironmentalTeam team)
    {
        var members = team.Members
            .OrderByDescending(m => m.IsLeader)
            .ThenBy(m => m.User?.FullName)
            .Select(m => new CompanyAssignmentTeamMember(
                m.UserId,
                m.User?.FullName,
                m.User?.AvatarUrl,
                m.IsLeader))
            .ToList();

        return new CompanyAssignmentTeam(
            team.Id,
            team.Name,
            members.Count,
            TeamWasteTagService.MapTags(team),
            members);
    }
}
