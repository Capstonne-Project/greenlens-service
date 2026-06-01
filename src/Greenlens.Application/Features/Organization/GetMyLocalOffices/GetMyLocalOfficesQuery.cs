using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetMyLocalOffices;

/// <summary>
/// Returns local offices under the department managed by the current officer.
/// Used on the DEO dashboard to see offices in their jurisdiction.
/// Supports search (name/ward/officer), filter (isOnboarded), and sort.
/// </summary>
public sealed record GetMyLocalOfficesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsOnboarded = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<Result<GetMyLocalOfficesResponse>>;
