using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Users.GetAllUsersWithPaged;

/// <summary>
/// Fetch users with pagination, search, and filtering. Admin only.
/// </summary>
public sealed class GetAllUsersWithPagedQueryHandler(IUserRepository users)
    : IRequestHandler<GetAllUsersWithPagedQuery, Result<PagedList<UserListItemDto>>>
{
    public async Task<Result<PagedList<UserListItemDto>>> Handle(
        GetAllUsersWithPagedQuery request,
        CancellationToken cancellationToken)
    {
        var query = users.QueryAsNoTracking();

        // ── Filter ──
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLowerInvariant();
            query = query.Where(u =>
                u.Email.Contains(search) ||
                u.FullName.ToLower().Contains(search) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
        }

        if (request.Role is not null)
            query = query.Where(u => u.Role == request.Role.Value);

        if (request.IsEmailVerified is not null)
            query = query.Where(u => u.IsEmailVerified == request.IsEmailVerified.Value);

        // ── Count + Page ──
        var totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Email,
                u.FullName,
                u.PhoneNumber,
                u.AvatarUrl,
                u.Role,
                u.IsEmailVerified,
                u.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalItems);

        return new PagedList<UserListItemDto>(items, pagination);
    }
}
