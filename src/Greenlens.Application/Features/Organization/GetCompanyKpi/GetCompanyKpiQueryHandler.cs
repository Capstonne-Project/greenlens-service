using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.GetOfficerKpi;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetCompanyKpi;

/// <summary>
/// BR-CMP-020: KPI công ty — task volume, SLA adherence, avg processing time.
/// Access: DEO/Admin specify CompanyId; CM auto-resolves own company (BR-CMP-021).
/// </summary>
public sealed class GetCompanyKpiQueryHandler(
    IReportAssignmentRepository assignments,
    IEnvironmentalTeamRepository teams,
    IEnvironmentalServiceCompanyRepository companies,
    ICompanyStaffRepository companyStaff,
    IReportRepository reports,
    ICurrentUser currentUser)
    : IRequestHandler<GetCompanyKpiQuery, Result<CompanyKpiResponse>>
{
    public async Task<Result<CompanyKpiResponse>> Handle(
        GetCompanyKpiQuery request,
        CancellationToken cancellationToken)
    {
        // ── Resolve companyId ──
        Guid companyId;

        if (currentUser.Role == "CompanyManager")
        {
            // CM always sees own company (BR-CMP-021)
            var staff = await companyStaff
                .GetByUserIdAsync(currentUser.UserId, cancellationToken)
                .ConfigureAwait(false);

            if (staff is null)
                return Errors.Organization.NotCompanyManager;

            companyId = staff.CompanyId;

            // If CompanyId is provided, ensure it matches
            if (request.CompanyId.HasValue && request.CompanyId.Value != companyId)
                return Errors.Organization.CrossCompanyAccess;
        }
        else
        {
            // DEO/Admin must specify
            if (!request.CompanyId.HasValue)
                return new Error("COMPANY_ID_REQUIRED",
                    "CompanyId là bắt buộc cho DEO/Admin.",
                    ErrorType.Validation);

            companyId = request.CompanyId.Value;
        }

        // ── Load company ──
        var company = await companies.GetByIdAsync(companyId, cancellationToken)
            .ConfigureAwait(false);

        if (company is null)
            return Errors.Organization.CompanyNotFound;

        // ── Resolve period ──
        var (from, to) = ResolvePeriod(request);

        // ── Get all teamIds belonging to this company ──
        var companyTeamIds = await teams.QueryAsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (companyTeamIds.Count == 0)
        {
            return new CompanyKpiResponse(
                companyId, company.Name, from, to,
                0, 0, 0, 0, 0m, 0m);
        }

        // ── Query assignments for these teams in period ──
        var assignmentQuery = assignments.QueryAsNoTracking()
            .Where(a => companyTeamIds.Contains(a.TeamId))
            .Where(a => a.AssignedAt >= from && a.AssignedAt <= to);

        var totalAssigned = await assignmentQuery
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalCompleted = await assignmentQuery
            .CountAsync(a => a.Status == AssignmentStatus.Completed, cancellationToken)
            .ConfigureAwait(false);

        var totalDeclined = await assignmentQuery
            .CountAsync(a => a.Status == AssignmentStatus.Declined, cancellationToken)
            .ConfigureAwait(false);

        // ── SLA compliance: completed assignments whose reports are NOT SLA-breached ──
        var completedAssignments = assignmentQuery
            .Where(a => a.Status == AssignmentStatus.Completed);

        var completedReportIds = await completedAssignments
            .Select(a => a.ReportId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var slaBreachedCount = completedReportIds.Count > 0
            ? await reports.QueryAsNoTracking()
                .CountAsync(r => completedReportIds.Contains(r.Id) && r.SlaResolveBreached,
                    cancellationToken)
                .ConfigureAwait(false)
            : 0;

        var completedOnTime = totalCompleted - slaBreachedCount;
        if (completedOnTime < 0) completedOnTime = 0;

        var slaComplianceRate = totalCompleted > 0
            ? Math.Round((decimal)completedOnTime / totalCompleted * 100, 1)
            : 0m;

        // ── Avg resolution time (hours): CompletedAt - AssignedAt ──
        var avgResolutionHours = 0m;
        if (totalCompleted > 0)
        {
            var completedWithDates = await completedAssignments
                .Where(a => a.CompletedAt != null)
                .Select(a => new { a.AssignedAt, a.CompletedAt })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (completedWithDates.Count > 0)
            {
                var totalHours = completedWithDates
                    .Sum(a => (a.CompletedAt!.Value - a.AssignedAt).TotalHours);
                avgResolutionHours = Math.Round((decimal)totalHours / completedWithDates.Count, 1);
            }
        }

        return new CompanyKpiResponse(
            companyId,
            company.Name,
            from,
            to,
            totalAssigned,
            totalCompleted,
            totalDeclined,
            completedOnTime,
            slaComplianceRate,
            avgResolutionHours);
    }

    private static (DateTime from, DateTime to) ResolvePeriod(GetCompanyKpiQuery request)
    {
        if (request.From.HasValue && request.To.HasValue)
            return (request.From.Value, request.To.Value);

        var now = DateTime.UtcNow;

        return request.Period switch
        {
            KpiPeriod.ThisMonth => (new DateTime(now.Year, now.Month, 1), now),
            KpiPeriod.LastMonth => (new DateTime(now.Year, now.Month, 1).AddMonths(-1),
                                   new DateTime(now.Year, now.Month, 1).AddSeconds(-1)),
            KpiPeriod.ThisQuarter => (GetQuarterStart(now), now),
            KpiPeriod.LastQuarter => (GetQuarterStart(now).AddMonths(-3),
                                     GetQuarterStart(now).AddSeconds(-1)),
            KpiPeriod.ThisYear => (new DateTime(now.Year, 1, 1), now),
            KpiPeriod.LastYear => (new DateTime(now.Year - 1, 1, 1),
                                  new DateTime(now.Year, 1, 1).AddSeconds(-1)),
            _ => (new DateTime(now.Year, now.Month, 1), now)
        };
    }

    private static DateTime GetQuarterStart(DateTime date)
    {
        var quarterMonth = ((date.Month - 1) / 3) * 3 + 1;
        return new DateTime(date.Year, quarterMonth, 1);
    }
}
