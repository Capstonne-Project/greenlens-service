using Greenlens.Application.Features.Analytics.GetAdminCompanyPerformance;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoCompanyPerformance;

public sealed record GetDeoCompanyPerformanceQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<CompanyPerformanceItem>>>;
