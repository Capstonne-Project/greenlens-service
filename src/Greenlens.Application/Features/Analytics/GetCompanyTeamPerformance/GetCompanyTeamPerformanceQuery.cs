using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetCompanyTeamPerformance;

public sealed record GetCompanyTeamPerformanceQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<TeamPerformanceItem>>>;

public sealed record TeamPerformanceItem(
    Guid TeamId,
    string TeamName,
    int AssignedTasks,
    int CompletedTasks,
    decimal CompletionRate,
    decimal AverageCompletionHours);
