using Greenlens.Application.Features.Reports.GetOfficerKpi;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.GetInspectionTeamKpi;

/// <summary>
/// BR-INS-032: KPI query for Inspection Team.
/// Metrics: penalty decision on-time %, paid on-time %, repeat offenders.
/// Supports custom From/To range AND preset periods (reuses KpiPeriod).
/// </summary>
public sealed record GetInspectionTeamKpiQuery(
    Guid? TeamId = null,
    DateTime? From = null,
    DateTime? To = null,
    KpiPeriod? Period = null) : IRequest<Result<InspectionTeamKpiResponse>>;

public sealed record InspectionTeamKpiResponse(
    Guid TeamId,
    string TeamName,
    DateTime PeriodFrom,
    DateTime PeriodTo,
    // Penalty decision KPIs
    int TotalInspections,
    int PenaltyIssuedCount,
    int PenaltyIssuedOnTime,
    decimal PenaltyIssuedOnTimePercent,
    int ClosedNoViolationCount,
    // Payment KPIs
    int TotalPaid,
    int PaidOnTime,
    decimal PaidOnTimePercent,
    // Repeat offenders
    int RepeatOffenderCount,
    // SLA
    int SlaBreach);
