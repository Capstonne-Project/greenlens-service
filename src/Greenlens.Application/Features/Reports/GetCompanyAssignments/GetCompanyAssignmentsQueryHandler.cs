using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
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
public sealed class GetCompanyAssignmentsQueryHandler(
    IReportAssignmentRepository assignments,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    ILogger<GetCompanyAssignmentsQueryHandler> logger)
    : IRequestHandler<GetCompanyAssignmentsQuery, Result<GetCompanyAssignmentsResponse>>
{
    public async Task<Result<GetCompanyAssignmentsResponse>> Handle(
        GetCompanyAssignmentsQuery request, CancellationToken ct)
    {
        // ── 1. Resolve caller's company ──
        var staff = await companyStaff.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (staff is null || !staff.IsActive)
            return Errors.Reports.ReportNotDispatchedToYourCompany;

        var companyId = staff.CompanyId;

        // ── 2. Build query: all assignments where team belongs to this company ──
        var baseQuery = assignments.QueryAsNoTracking()
            .Include(a => a.Report).ThenInclude(r => r!.Category)
            .Include(a => a.Team).ThenInclude(t => t!.Members)
            .Include(a => a.AssignedByUser)
            .Where(a => a.Team!.CompanyId == companyId);

        // Filter by assignment status
        if (request.Status.HasValue)
            baseQuery = baseQuery.Where(a => a.Status == request.Status.Value);

        // Filter by report status
        if (request.ReportStatus.HasValue)
            baseQuery = baseQuery.Where(a => a.Report!.Status == request.ReportStatus.Value);

        // Search by report code or address
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(a =>
                a.Report!.Code.ToLower().Contains(search) ||
                (a.Report.Address != null && a.Report.Address.ToLower().Contains(search)) ||
                a.Team!.Name.ToLower().Contains(search));
        }

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var items = await baseQuery
            .OrderByDescending(a => a.AssignedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new CompanyAssignmentItem(
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
                    a.Report.SlaResolveDueAt),
                new CompanyAssignmentTeam(
                    a.Team!.Id,
                    a.Team.Name,
                    a.Team.Members.Count),
                a.AssignedByUser != null ? a.AssignedByUser.FullName : "Unknown"))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, total);

        logger.LogInformation(
            "CM {UserId} viewed company assignments: {Count}/{Total} for company {CompanyId}",
            currentUser.UserId, items.Count, total, companyId);

        return new GetCompanyAssignmentsResponse(items, pagination);
    }
}
