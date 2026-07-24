using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminOverview;

public sealed record GetAdminOverviewQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<AdminOverviewResponse>>;

public sealed record AdminOverviewResponse(
    int TotalUsers,
    int TotalReports,
    int PendingReports,
    int ResolvedReports,
    int ActiveCompanies,
    int ActiveTeams,
    decimal SlaComplianceRate,
    decimal AverageResolutionHours);
