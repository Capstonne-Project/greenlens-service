using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminCompanyPerformance;

public sealed record GetAdminCompanyPerformanceQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<CompanyPerformanceItem>>>;

public sealed record CompanyPerformanceItem(
    Guid CompanyId,
    string CompanyName,
    int AssignedTasks,
    int CompletedTasks,
    decimal OnTimeRate,
    decimal SlaRate,
    decimal PerformanceScore);
