using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetOfficerKpi;

/// <summary>
/// BR-OFF-021: KPI query for Officers.
/// Supports custom From/To range AND preset periods (ThisMonth, ThisQuarter, ThisYear).
/// LEO sees own KPI; DEO/Admin can query any officer.
/// </summary>
public sealed record GetOfficerKpiQuery(
    Guid? OfficerId = null,
    DateTime? From = null,
    DateTime? To = null,
    KpiPeriod? Period = null) : IRequest<Result<OfficerKpiResponse>>;

/// <summary>Preset KPI time periods.</summary>
public enum KpiPeriod
{
    ThisMonth,
    ThisQuarter,
    ThisYear,
    LastMonth,
    LastQuarter,
    LastYear
}

public sealed record OfficerKpiResponse(
    Guid OfficerId,
    string OfficerName,
    DateTime PeriodFrom,
    DateTime PeriodTo,
    // Verification KPIs
    int TotalVerified,
    int VerifiedOnTime,
    decimal VerifiedOnTimePercent,
    int TotalRejected,
    int TotalEscalated,
    // Resolution KPIs
    int TotalResolved,
    int TotalClosed,
    decimal ResolvedRate,
    // Response time
    decimal AvgResponseTimeHours);
