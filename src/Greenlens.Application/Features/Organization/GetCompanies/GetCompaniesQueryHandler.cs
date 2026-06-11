using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetCompanies;

/// <summary>
/// Returns a paginated list of companies with search/filter/sort support.
/// </summary>
public sealed class GetCompaniesQueryHandler(
    IEnvironmentalServiceCompanyRepository companies)
    : IRequestHandler<GetCompaniesQuery, Result<GetCompaniesResponse>>
{
    public async Task<Result<GetCompaniesResponse>> Handle(
        GetCompaniesQuery request,
        CancellationToken ct)
    {
        var query = companies.QueryAsNoTracking()
            .Include(c => c.ServiceAreas)
            .Include(c => c.Staff)
            .AsQueryable();

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

        var pagination = PaginationMeta.Create(totalCount, request.Page, request.PageSize);

        return new GetCompaniesResponse(items, pagination);
    }
}
