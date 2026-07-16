using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.GetOfficerKpi;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Inspection.GetInspectionTeamKpi;

/// <summary>
/// BR-INS-032: Compute KPI metrics for an Inspection Team within a time period.
/// Metrics: penalty on-time %, paid on-time %, repeat offenders, SLA breaches.
/// </summary>
public sealed class GetInspectionTeamKpiQueryHandler(
    IInspectionReportRepository inspectionRepo,
    IEnvironmentalTeamRepository teams,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser)
    : IRequestHandler<GetInspectionTeamKpiQuery, Result<InspectionTeamKpiResponse>>
{
    public async Task<Result<InspectionTeamKpiResponse>> Handle(
        GetInspectionTeamKpiQuery request,
        CancellationToken ct)
    {
        // Resolve team ID — Inspector sees own team, LEO/Admin can specify
        Guid teamId;
        if (request.TeamId.HasValue)
        {
            teamId = request.TeamId.Value;
        }
        else
        {
            var member = await teamMembers.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
            if (member is null)
                return Errors.Inspections.NotAssignedToYourTeam;
            teamId = member.TeamId;
        }

        var team = await teams.GetByIdAsync(teamId, ct).ConfigureAwait(false);
        if (team is null)
            return Errors.Inspections.TeamNotFound;

        // Resolve period
        var (from, to) = ResolvePeriod(request);

        // Query inspections for this team in period
        var inspections = await inspectionRepo.QueryAsNoTracking()
            .Where(ir => ir.AssignedTeamId == teamId
                      && ir.CreatedAt >= from
                      && ir.CreatedAt <= to)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var totalInspections = inspections.Count;

        // Penalty issued
        var penaltyIssued = inspections
            .Where(ir => ir.Status is InspectionStatus.PenaltyIssued
                or InspectionStatus.Paid
                or InspectionStatus.PartiallyPaid
                or InspectionStatus.Overdue
                or InspectionStatus.Closed)
            .ToList();

        var penaltyIssuedCount = penaltyIssued.Count;

        // On-time = penalty issued before SLA deadline
        var penaltyIssuedOnTime = penaltyIssued
            .Count(ir => ir.PenaltyIssuedAt.HasValue
                      && ir.SlaInspectionDueAt.HasValue
                      && ir.PenaltyIssuedAt <= ir.SlaInspectionDueAt);

        // Closed no violation
        var closedNoViolation = inspections
            .Count(ir => ir.Status == InspectionStatus.ClosedNoViolation);

        // Payment
        var paidInspections = inspections
            .Where(ir => ir.Status is InspectionStatus.Paid or InspectionStatus.Closed)
            .ToList();

        var totalPaid = paidInspections.Count;
        var paidOnTime = paidInspections
            .Count(ir => ir.PenaltyDueDate.HasValue
                      && ir.ClosedAt.HasValue
                      && ir.ClosedAt <= ir.PenaltyDueDate);

        // Repeat offenders
        var repeatOffenders = inspections.Count(ir => ir.IsRepeatOffender);

        // SLA breaches
        var slaBreaches = inspections.Count(ir => ir.SlaInspectionBreached);

        return new InspectionTeamKpiResponse(
            teamId,
            team.Name,
            from,
            to,
            totalInspections,
            penaltyIssuedCount,
            penaltyIssuedOnTime,
            penaltyIssuedCount > 0
                ? Math.Round((decimal)penaltyIssuedOnTime / penaltyIssuedCount * 100, 1)
                : 0m,
            closedNoViolation,
            totalPaid,
            paidOnTime,
            totalPaid > 0
                ? Math.Round((decimal)paidOnTime / totalPaid * 100, 1)
                : 0m,
            repeatOffenders,
            slaBreaches);
    }

    private static (DateTime from, DateTime to) ResolvePeriod(GetInspectionTeamKpiQuery request)
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

    private static DateTime Utc(int year, int month, int day)
        => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime GetQuarterStart(DateTime date)
    {
        var quarterMonth = ((date.Month - 1) / 3) * 3 + 1;
        return new DateTime(date.Year, quarterMonth, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
