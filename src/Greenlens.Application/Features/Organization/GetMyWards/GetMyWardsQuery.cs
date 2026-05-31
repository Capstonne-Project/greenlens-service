using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetMyWards;

/// <summary>
/// Returns wards/communes under the province managed by the current officer's department.
/// Used on the DEO/LEO dashboard to populate ward dropdowns scoped to their jurisdiction.
/// </summary>
public sealed record GetMyWardsQuery : IRequest<Result<GetMyWardsResponse>>;
