using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Users.GetAllUsersWithPaged;

/// <summary>
/// Fetch users with pagination, search, and filtering. Admin only.
/// </summary>
public sealed class GetAllUsersWithPagedQueryHandler(IUserRepository users,
    ILogger<GetAllUsersWithPagedQueryHandler> logger)
    : IRequestHandler<GetAllUsersWithPagedQuery, Result<PagedList<UserListItemDto>>>
{
    public async Task<Result<PagedList<UserListItemDto>>> Handle(
        GetAllUsersWithPagedQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting all users with pagination, search, and filtering");

        var query = users.QueryAsNoTracking();

        // ── Filter ──
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            logger.LogInformation("Searching for users with search term {Search}", request.Search);
            var search = request.Search.Trim();
            // FullName: no ToLower — PostgreSQL lower() with C locale skips Unicode (Đ/đ), while C# lowercases the term.
            var searchLower = search.ToLowerInvariant();
            query = query.Where(u =>
                u.Email.ToLower().Contains(searchLower) ||
                u.FullName.Contains(search) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
        }

        if (request.Role is not null)
        {
            logger.LogInformation("Filtering users with role {Role}", request.Role.Value);
            query = query.Where(u => u.Role == request.Role.Value);
        }

        if (request.IsEmailVerified is not null)
        {
            logger.LogInformation("Filtering users with email verified {IsEmailVerified}", request.IsEmailVerified.Value);
            query = query.Where(u => u.IsEmailVerified == request.IsEmailVerified.Value);
        }

        // ── Count + Page ──
        logger.LogInformation("Counting users");
        var totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Total users: {TotalItems}", totalItems);

        logger.LogInformation("Getting users with pagination");
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

        logger.LogInformation("Lấy danh sách người dùng thành công. Số lượng: {Count}", items.Count);
        return new PagedList<UserListItemDto>(items, pagination);
    }
}
