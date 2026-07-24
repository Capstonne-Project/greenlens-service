using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetCompanyOverview;

public sealed record GetCompanyOverviewQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<CompanyOverviewResponse>>;

public sealed record CompanyOverviewResponse(
    int AssignedTasks,
    int CompletedTasks,
    int PendingTasks,
    int ActiveTeams,
    int ActiveStaff,
    decimal SlaComplianceRate,
    decimal AverageResolutionHours);
