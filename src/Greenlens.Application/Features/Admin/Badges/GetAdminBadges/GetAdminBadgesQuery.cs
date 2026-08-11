using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.Badges.GetAdminBadges;

/// <summary>Returns all badges (including inactive) for Admin Dashboard.</summary>
/// <remarks>Implements: BR-ADM-005.</remarks>
public sealed record GetAdminBadgesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<Result<GetAdminBadgesResponse>>;
