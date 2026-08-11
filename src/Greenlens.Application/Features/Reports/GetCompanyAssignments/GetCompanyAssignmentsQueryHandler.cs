using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Analytics.Common;
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

        logger.LogInformation("Company ID: {CompanyId}", companyId);

        var baseQuery = assignments.QueryAsNoTracking()
            .Include(a => a.Report).ThenInclude(r => r!.Category)
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
            logger.LogInformation("Searching by report code or address: {Search}", request.Search);
            var search = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(a =>
                a.Report!.Code.ToLower().Contains(search) ||
                (a.Report.Address != null && a.Report.Address.ToLower().Contains(search)) ||
                a.Team!.Name.ToLower().Contains(search));
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

        var pageAssignments = await baseQuery
            .OrderByDescending(a => a.AssignedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, total);

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

        return new CompanyAssignmentTeam(team.Id, team.Name, members.Count, members);
    }
}
