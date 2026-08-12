using Greenlens.Application.Features.Analytics.GetAdminGeographic;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoGeographic;

public sealed record GetDeoGeographicQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<GeographicResponse>>;
