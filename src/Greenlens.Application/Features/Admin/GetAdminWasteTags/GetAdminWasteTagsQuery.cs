using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.GetAdminWasteTags;

/// <summary>
/// Returns all waste tags (including inactive) for Admin Dashboard.
/// Supports search, filter by isActive, sort, and pagination.
/// </summary>
public sealed record GetAdminWasteTagsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<Result<GetAdminWasteTagsResponse>>;
