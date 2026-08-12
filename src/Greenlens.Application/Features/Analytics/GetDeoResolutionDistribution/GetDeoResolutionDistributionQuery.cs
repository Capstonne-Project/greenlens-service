using Greenlens.Application.Features.Analytics.GetAdminResolutionDistribution;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoResolutionDistribution;

public sealed record GetDeoResolutionDistributionQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<ResolutionDistributionBucket>>>;
