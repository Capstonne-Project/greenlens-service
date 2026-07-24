using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminResolutionDistribution;

public sealed record GetAdminResolutionDistributionQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<ResolutionDistributionBucket>>>;

public sealed record ResolutionDistributionBucket(
    string Range,
    int Count);
