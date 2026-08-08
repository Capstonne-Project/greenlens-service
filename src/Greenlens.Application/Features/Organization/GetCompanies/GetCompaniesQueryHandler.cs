using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetCompanies;

/// <summary>
/// Returns a paginated list of companies with search/filter/sort support.
/// DEO sees only companies with service areas in their province (BR-ADM-012).
/// Admin sees all companies.
/// </summary>
/// <remarks>Implements: BR-CMP-001, BR-ADM-012.</remarks>
public sealed class GetCompaniesQueryHandler(
    IEnvironmentalServiceCompanyRepository companies,
    ICurrentUser currentUser,
    IUserRepository users,
    ILogger<GetCompaniesQueryHandler> logger)
    : IRequestHandler<GetCompaniesQuery, Result<GetCompaniesResponse>>
{
    public async Task<Result<GetCompaniesResponse>> Handle(
        GetCompaniesQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting companies for user {UserId}", currentUser.UserId);

        var query = companies.QueryAsNoTracking()
            .Include(c => c.ServiceAreas)
            .Include(c => c.Staff)
            .AsQueryable();

        // ── BR-ADM-012: DEO scope by department ──
        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("User not found for companies list: {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        if (user.Role == UserRole.DEO)
        {
            if (!user.DepartmentId.HasValue)
            {
                logger.LogWarning("DEO {UserId} has no department", currentUser.UserId);
                return Errors.Organization.DepartmentNotFound;
            }

            logger.LogInformation(
                "DEO {UserId} listing companies for department {DepartmentId}",
                currentUser.UserId, user.DepartmentId.Value);

            query = query.Where(c => c.DepartmentId == user.DepartmentId.Value);
        }

        // ── Filter ──
        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.ContractNumber.ToLower().Contains(term) ||
                (c.TaxCode != null && c.TaxCode.ToLower().Contains(term)));
        }

        // ── Sort ──
        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDesc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "status" => request.SortDesc ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "contractnumber" => request.SortDesc ? query.OrderByDescending(c => c.ContractNumber) : query.OrderBy(c => c.ContractNumber),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CompanyListItem(
                c.Id,
                c.Name,
                c.ContractNumber,
                c.ContractType.ToString(),
                c.Status.ToString(),
                c.ContractStartDate,
                c.ContractEndDate,
                c.TaxCode,
                c.Phone,
                c.Email,
                c.ServiceAreas.Count,
                c.Staff.Count,
                c.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        logger.LogInformation("Companies found: {TotalCount}", totalCount);

        return new GetCompaniesResponse(items, pagination);
    }
}
