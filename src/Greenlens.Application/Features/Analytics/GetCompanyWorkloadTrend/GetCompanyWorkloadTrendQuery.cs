using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetCompanyWorkloadTrend;

public sealed record GetCompanyWorkloadTrendQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<WorkloadTrendItem>>>;

public sealed record WorkloadTrendItem(
    DateOnly Date,
    int Assigned,
    int Completed);
