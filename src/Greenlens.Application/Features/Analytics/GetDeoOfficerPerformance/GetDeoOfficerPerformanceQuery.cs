using Greenlens.Application.Features.Analytics.GetAdminOfficerPerformance;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoOfficerPerformance;

public sealed record GetDeoOfficerPerformanceQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<OfficerPerformanceItem>>>;
