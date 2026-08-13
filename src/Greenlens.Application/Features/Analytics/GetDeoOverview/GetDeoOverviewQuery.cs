using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoOverview;

/// <summary>Province-scoped overview KPIs for the DEO monitoring dashboard.</summary>
public sealed record GetDeoOverviewQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<DeoOverviewResponse>>;

public sealed record DeoOverviewResponse(
    Guid DepartmentId,
    string DepartmentName,
    int TotalReports,
    int PendingReports,
    int ResolvedReports,
    int SlaBreachedCount,
    int DuplicateFlagCount,
    int RecurrenceFlagCount,
    decimal SlaComplianceRate,
    decimal AverageResolutionHours,
    int ActiveCompanies,
    int PendingActivationCompanies,
    int LocalOfficeCount,
    int OnboardedOfficeCount);
