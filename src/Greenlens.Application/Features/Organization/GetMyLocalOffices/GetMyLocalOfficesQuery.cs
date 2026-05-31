using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetMyLocalOffices;

/// <summary>
/// Returns local offices under the department managed by the current officer.
/// Used on the DEO dashboard to see offices in their jurisdiction.
/// </summary>
public sealed record GetMyLocalOfficesQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<Result<GetMyLocalOfficesResponse>>;
