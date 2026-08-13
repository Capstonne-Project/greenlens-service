using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetMyWardBoundary;

/// <summary>Ward boundary of the current LEO's own office, resolved from the JWT — no params needed.</summary>
public sealed record GetMyWardBoundaryQuery : IRequest<Result<GetMyWardBoundaryResponse>>;
