using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Application.Features.Organization.GetCompanyById;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetOfficeCompanyById;

/// <summary>
/// Returns company detail when the company serves the LEO's ward.
/// </summary>
/// <remarks>Implements: BR-CMP-001, BR-CMP-008, BR-ORG-003.</remarks>
public sealed class GetOfficeCompanyByIdQueryHandler(
    ICurrentUser currentUser,
    IUserRepository users,
    ILocalOfficeRepository offices,
    IEnvironmentalServiceCompanyRepository companies,
    IEnvironmentalTeamRepository teams,
    IReportRepository reports,
    ILogger<GetOfficeCompanyByIdQueryHandler> logger)
    : IRequestHandler<GetOfficeCompanyByIdQuery, Result<OfficeCompanyDetailResponse>>
{
    private static readonly ReportStatus[] ActiveReportStatuses =
    [
        ReportStatus.Verified,
        ReportStatus.InProgress,
        ReportStatus.Resolved
    ];

    public async Task<Result<OfficeCompanyDetailResponse>> Handle(
        GetOfficeCompanyByIdQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting ward company {CompanyId} for LEO {UserId}", request.Id, currentUser.UserId);

        var scopeResult = await LeoOfficeScope.ResolveAsync(users, offices, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (!scopeResult.IsSuccess)
            return scopeResult.Error!;

        var office = scopeResult.Value!.Office;
        var wardCode = office.WardCode;

        // Cùng filter SQL như GetOfficeCompanies — tránh 404 khi Include+ThenInclude(Ward) không hydrate ServiceAreas.
        var company = await companies.QueryAsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.Staff)
            .Where(c => c.Id == request.Id)
            .Where(c => c.ServiceAreas.Any(sa => sa.WardCode == wardCode))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (company is null)
        {
            logger.LogWarning(
                "Company {CompanyId} not found or does not serve ward {WardCode}",
                request.Id, wardCode);
            return Errors.Organization.CompanyNotFound;
        }

        var accessError = CompanyAccessAuthorization.ValidateLeoViewAccess(scopeResult.Value.Leo);
        if (accessError is not null)
        {
            logger.LogWarning(
                "LEO {UserId} denied company {CompanyId}: {ErrorCode}",
                currentUser.UserId, request.Id, accessError.Code);
            return accessError;
        }

        // Projection join Ward trong SQL — không phụ thuộc ThenInclude char(5) FK.
        var serviceAreas = await companies.QueryAsNoTracking()
            .Where(c => c.Id == request.Id)
            .SelectMany(c => c.ServiceAreas)
            .Select(sa => new CompanyServiceAreaDto(
                sa.Id,
                sa.WardCode,
                sa.Ward != null ? sa.Ward.Name : sa.WardCode,
                sa.Ward != null ? sa.Ward.ProvinceCode : ""))
            .OrderBy(sa => sa.WardName)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var wardServiceArea = serviceAreas.First(sa => sa.WardCode == wardCode);

        var activeReportCount = await reports.QueryAsNoTracking()
            .Where(r => r.AssignedOfficeId == office.Id)
            .Where(r => r.AssignedCompanyId == company.Id)
            .Where(r => ActiveReportStatuses.Contains(r.Status))
            .CountAsync(ct)
            .ConfigureAwait(false);

        var completedReportCount = await reports.QueryAsNoTracking()
            .Where(r => r.AssignedOfficeId == office.Id)
            .Where(r => r.AssignedCompanyId == company.Id)
            .Where(r => r.Status == ReportStatus.Closed)
            .CountAsync(ct)
            .ConfigureAwait(false);

        var teamCount = await teams.QueryAsNoTracking()
            .Where(t => t.CompanyId == company.Id && t.IsActive)
            .CountAsync(ct)
            .ConfigureAwait(false);

        return new OfficeCompanyDetailResponse(
            company.Id,
            company.Name,
            company.ContractNumber,
            company.ContractType.ToString(),
            company.Status.ToString(),
            company.ContractStartDate,
            company.ContractEndDate,
            company.TaxCode,
            company.Address,
            company.Phone,
            company.Email,
            company.DepartmentId,
            company.Department?.Name,
            company.ActivatedAt,
            office.Id,
            office.Name,
            wardCode,
            office.Ward?.Name ?? wardCode,
            wardServiceArea,
            serviceAreas,
            company.Staff.Count,
            teamCount,
            activeReportCount,
            completedReportCount,
            company.CreatedAt);
    }
}
