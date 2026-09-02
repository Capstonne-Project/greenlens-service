using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Organization.Common;
using Greenlens.Application.Features.Organization.GetCompanyById;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities.Location;
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
    IWardRepository wards,
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
        var wardCode = office.WardCode.Trim();

        // Include ServiceAreas (không ThenInclude Ward) — tránh join char(5) làm collection rỗng.
        var company = await companies.QueryAsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.ServiceAreas)
            .Include(c => c.Staff)
            .Where(c => c.Id == request.Id)
            .Where(c => c.ServiceAreas.Any(sa => sa.WardCode.Trim() == wardCode))
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

        // Lookup tên phường riêng — Ward.Code char(5) không join ổn định trong projection.
        var wardCodes = company.ServiceAreas
            .Select(sa => sa.WardCode.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var wardLookup = new Dictionary<string, Ward>(StringComparer.Ordinal);
        foreach (var code in wardCodes)
        {
            var ward = await wards.GetByCodeAsync(code, ct).ConfigureAwait(false);
            if (ward is not null)
                wardLookup[code] = ward;
        }

        var serviceAreas = company.ServiceAreas
            .Select(sa =>
            {
                var code = sa.WardCode.Trim();
                wardLookup.TryGetValue(code, out var ward);
                return new CompanyServiceAreaDto(
                    sa.Id,
                    code,
                    ward?.Name ?? code,
                    ward?.ProvinceCode.Trim() ?? "");
            })
            .OrderBy(sa => sa.WardName)
            .ToList();

        var wardServiceArea = serviceAreas.FirstOrDefault(sa => sa.WardCode == wardCode);
        if (wardServiceArea is null)
        {
            logger.LogWarning(
                "Ward service area {WardCode} missing for company {CompanyId} after load",
                wardCode, request.Id);
            return Errors.Organization.CompanyNotFound;
        }

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
