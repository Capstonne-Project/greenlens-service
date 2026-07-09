using Greenlens.Application.Features.Reports.GetOfficerKpi;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetCompanyKpi;

/// <summary>
/// BR-CMP-020: KPI công ty — DEO xem theo tỉnh, CM xem công ty mình.
/// Reuses KpiPeriod enum from GetOfficerKpi.
/// </summary>
public sealed record GetCompanyKpiQuery(
    Guid? CompanyId = null,
    DateTime? From = null,
    DateTime? To = null,
    KpiPeriod? Period = null) : IRequest<Result<CompanyKpiResponse>>;

public sealed record CompanyKpiResponse(
    Guid CompanyId,
    string CompanyName,
    DateTime PeriodFrom,
    DateTime PeriodTo,
    int TotalAssigned,
    int TotalCompleted,
    int TotalDeclined,
    int CompletedOnTime,
    decimal SlaComplianceRate,
    decimal AvgResolutionHours);
