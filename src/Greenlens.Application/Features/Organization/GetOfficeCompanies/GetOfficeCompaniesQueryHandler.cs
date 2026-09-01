using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetOfficeCompanies;

/// <summary>
/// Returns paginated companies serving the LEO's ward with search, filter, and sort.
/// </summary>
/// <remarks>Implements: BR-CMP-005, BR-CMP-008, BR-ORG-003.</remarks>
public sealed class GetOfficeCompaniesQueryHandler(
    ICurrentUser currentUser,
    IUserRepository users,
    ILocalOfficeRepository offices,
    IEnvironmentalServiceCompanyRepository companies,
    IReportRepository reports,
    ILogger<GetOfficeCompaniesQueryHandler> logger)
    : IRequestHandler<GetOfficeCompaniesQuery, Result<GetOfficeCompaniesResponse>>
{
    private static readonly ReportStatus[] ActiveReportStatuses =
    [
        ReportStatus.Verified,
        ReportStatus.InProgress,
        ReportStatus.Resolved
    ];

    public async Task<Result<GetOfficeCompaniesResponse>> Handle(
        GetOfficeCompaniesQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting ward companies for LEO {UserId}", currentUser.UserId);

        var scopeResult = await LeoOfficeScope.ResolveAsync(users, offices, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (!scopeResult.IsSuccess)
            return scopeResult.Error!;

        var office = scopeResult.Value!.Office;
        var wardCode = office.WardCode;

        var query = companies.QueryAsNoTracking()
            .Include(c => c.ServiceAreas)
            .Include(c => c.Staff)
            .Where(c => c.ServiceAreas.Any(sa => sa.WardCode == wardCode));

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        if (request.ContractType.HasValue)
            query = query.Where(c => c.ContractType == request.ContractType.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.ContractNumber.ToLower().Contains(term) ||
                (c.TaxCode != null && c.TaxCode.ToLower().Contains(term)) ||
                (c.Phone != null && c.Phone.ToLower().Contains(term)) ||
                (c.Email != null && c.Email.ToLower().Contains(term)));
        }

        query = request.SortBy?.Trim().ToLowerInvariant() switch
        {
            "name" => request.SortDesc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "status" => request.SortDesc ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "contractnumber" => request.SortDesc
                ? query.OrderByDescending(c => c.ContractNumber)
                : query.OrderBy(c => c.ContractNumber),
            "staffcount" => request.SortDesc
                ? query.OrderByDescending(c => c.Staff.Count)
                : query.OrderBy(c => c.Staff.Count),
            "createdat" => request.SortDesc
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt),
            _ => query.OrderBy(c => c.Name)
        };

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var pageCompanies = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.ContractNumber,
                ContractType = c.ContractType.ToString(),
                Status = c.Status.ToString(),
                c.ContractStartDate,
                c.ContractEndDate,
                c.TaxCode,
                c.Phone,
                c.Email,
                ServiceAreaCount = c.ServiceAreas.Count,
                StaffCount = c.Staff.Count,
                c.CreatedAt
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (pageCompanies.Count == 0)
        {
            return new GetOfficeCompaniesResponse(
                office.Id,
                office.Name,
                wardCode,
                office.Ward?.Name ?? wardCode,
                [],
                pagination);
        }

        var companyIds = pageCompanies.Select(c => c.Id).ToList();
        var activeCounts = await reports.QueryAsNoTracking()
            .Where(r => r.AssignedOfficeId == office.Id)
            .Where(r => r.AssignedCompanyId != null && companyIds.Contains(r.AssignedCompanyId.Value))
            .Where(r => ActiveReportStatuses.Contains(r.Status))
            .GroupBy(r => r.AssignedCompanyId!.Value)
            .Select(g => new { CompanyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CompanyId, x => x.Count, ct)
            .ConfigureAwait(false);

        var items = pageCompanies
            .Select(c => new OfficeCompanyItem(
                c.Id,
                c.Name,
                c.ContractNumber,
                c.ContractType,
                c.Status,
                c.ContractStartDate,
                c.ContractEndDate,
                c.TaxCode,
                c.Phone,
                c.Email,
                c.ServiceAreaCount,
                c.StaffCount,
                activeCounts.GetValueOrDefault(c.Id),
                c.CreatedAt))
            .ToList();

        logger.LogInformation(
            "Ward companies for office {OfficeId}: {Count}/{Total}",
            office.Id, items.Count, totalCount);

        return new GetOfficeCompaniesResponse(
            office.Id,
            office.Name,
            wardCode,
            office.Ward?.Name ?? wardCode,
            items,
            pagination);
    }
}
