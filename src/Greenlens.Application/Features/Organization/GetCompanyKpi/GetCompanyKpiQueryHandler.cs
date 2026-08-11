using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.GetOfficerKpi;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    IUserRepository users,
    IReportRepository reports,
    ICurrentUser currentUser,
    ILogger<GetCompanyKpiQueryHandler> logger)
    : IRequestHandler<GetCompanyKpiQuery, Result<CompanyKpiResponse>>
{
    public async Task<Result<CompanyKpiResponse>> Handle(
        GetCompanyKpiQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting company KPI for user {UserId}", currentUser.UserId);

        // ── Resolve companyId ──
        Guid companyId;

        if (currentUser.Role == "CompanyManager")
        {
            // CM always sees own company (BR-CMP-021)
            var staff = await companyStaff
                .GetByUserIdAsync(currentUser.UserId, cancellationToken)
                .ConfigureAwait(false);

            if (staff is null || !staff.IsActive)
            {
                logger.LogWarning("Company manager not found or inactive for user ID {UserId}", currentUser.UserId);
                return Errors.Organization.NotCompanyManager;
            }

            companyId = staff.CompanyId;

            // If CompanyId is provided, ensure it matches
            if (request.CompanyId.HasValue && request.CompanyId.Value != companyId)
            {
                logger.LogWarning("Company ID {CompanyId} does not match user's company ID {CompanyId}", request.CompanyId.Value, companyId);
                return Errors.Organization.CrossCompanyAccess;
            }
        }
        else
        {
            // DEO/Admin must specify
            if (!request.CompanyId.HasValue)
            {
                logger.LogWarning("Company ID is required");
                return Errors.Organization.CompanyIdRequired;
            }

            companyId = request.CompanyId.Value;
        }

        // ── Load company ──
        var company = await companies.GetByIdAsync(companyId, cancellationToken)
            .ConfigureAwait(false);

        if (company is null)
        {
            logger.LogWarning("Company {CompanyId} not found", companyId);
            return Errors.Organization.CompanyNotFound;
        }

        if (currentUser.Role == UserRole.DEO.ToString())
        {
            var deo = await users.GetByIdAsync(currentUser.UserId, cancellationToken).ConfigureAwait(false);
            if (deo is null)
            {
                logger.LogWarning("User not found for company KPI: {UserId}", currentUser.UserId);
                return Errors.Users.UserNotFound;
            }

            var accessError = CompanyAccessAuthorization.ValidateViewAccess(company, deo);
            if (accessError is not null)
            {
                logger.LogWarning(
                    "User {UserId} denied KPI for company {CompanyId}: {ErrorCode}",
                    currentUser.UserId, companyId, accessError.Code);
                return accessError;
            }
        }

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
            logger.LogWarning("No teams found for company {CompanyId}", companyId);
            return new CompanyKpiResponse(companyId, company.Name, from, to, 0, 0, 0, 0, 0m, 0m);
        }

        logger.LogInformation("Company {CompanyId} has {CompanyTeamIds} teams", companyId, companyTeamIds.Count);

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

        logger.LogInformation("Completed on time: {CompletedOnTime}", completedOnTime);

        var slaComplianceRate = totalCompleted > 0
            ? Math.Round((decimal)completedOnTime / totalCompleted * 100, 1)
            : 0m;

        logger.LogInformation("SLA compliance rate: {SlaComplianceRate}", slaComplianceRate);

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

        logger.LogInformation("Average resolution hours: {AvgResolutionHours}", avgResolutionHours);

        logger.LogInformation("Company KPI response: {CompanyId}, {Name}, {From}, {To}, {TotalAssigned}, {TotalCompleted}, {TotalDeclined}, {CompletedOnTime}, {SlaComplianceRate}, {AvgResolutionHours}",
            companyId, company.Name, from, to, totalAssigned, totalCompleted, totalDeclined, completedOnTime, slaComplianceRate, avgResolutionHours);

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
            return (DateTime.SpecifyKind(request.From.Value, DateTimeKind.Utc),
                    DateTime.SpecifyKind(request.To.Value, DateTimeKind.Utc));

        var now = DateTime.UtcNow;

        return request.Period switch
        {
            KpiPeriod.ThisMonth => (Utc(now.Year, now.Month, 1), now),
            KpiPeriod.LastMonth => (Utc(now.Year, now.Month, 1).AddMonths(-1),
                                   Utc(now.Year, now.Month, 1).AddSeconds(-1)),
            KpiPeriod.ThisQuarter => (GetQuarterStart(now), now),
            KpiPeriod.LastQuarter => (GetQuarterStart(now).AddMonths(-3),
                                     GetQuarterStart(now).AddSeconds(-1)),
            KpiPeriod.ThisYear => (Utc(now.Year, 1, 1), now),
            KpiPeriod.LastYear => (Utc(now.Year - 1, 1, 1),
                                  Utc(now.Year, 1, 1).AddSeconds(-1)),
            _ => (Utc(now.Year, now.Month, 1), now)
        };
    }

    /// <summary>Shorthand for creating a UTC DateTime.</summary>
    private static DateTime Utc(int year, int month, int day)
        => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime GetQuarterStart(DateTime date)
    {
        var quarterMonth = ((date.Month - 1) / 3) * 3 + 1;
        return new DateTime(date.Year, quarterMonth, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
