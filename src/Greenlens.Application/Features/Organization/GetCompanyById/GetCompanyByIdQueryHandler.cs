using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetCompanyById;

/// <summary>
/// Returns full company detail with service areas and staff count.
/// Scope: DEO → own department; Admin → all.
/// </summary>
/// <remarks>Implements: BR-ADM-012, BR-CMP-001.</remarks>
public sealed class GetCompanyByIdQueryHandler(
    IEnvironmentalServiceCompanyRepository companies,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetCompanyByIdQueryHandler> logger)
    : IRequestHandler<GetCompanyByIdQuery, Result<CompanyDetailResponse>>
{
    public async Task<Result<CompanyDetailResponse>> Handle(
        GetCompanyByIdQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting company by ID {Id}", request.Id);

        var actor = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (actor is null)
        {
            logger.LogWarning("User not found for company detail: {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        var company = await companies.QueryAsNoTracking()
            .Include(c => c.Department)
            .Include(c => c.ServiceAreas)
                .ThenInclude(sa => sa.Ward)
            .Include(c => c.Staff)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (company is null)
        {
            logger.LogWarning("Company {Id} not found", request.Id);
            return Errors.Organization.CompanyNotFound;
        }

        var accessError = CompanyAccessAuthorization.ValidateViewAccess(company, actor);
        if (accessError is not null)
        {
            logger.LogWarning(
                "User {UserId} denied company detail {CompanyId}: {ErrorCode}",
                currentUser.UserId, request.Id, accessError.Code);
            return accessError;
        }

        var serviceAreas = company.ServiceAreas
            .OrderBy(sa => sa.Ward?.Name)
            .Select(sa => new CompanyServiceAreaDto(
                sa.Id,
                sa.WardCode,
                sa.Ward?.Name ?? sa.WardCode,
                sa.Ward?.ProvinceCode ?? ""))
            .ToList();

        logger.LogInformation("Company {Id} found", request.Id);
        logger.LogInformation("Service areas: {ServiceAreas}", serviceAreas);
        logger.LogInformation("Staff count: {StaffCount}", company.Staff.Count);
        logger.LogInformation("Created at: {CreatedAt}", company.CreatedAt);

        return new CompanyDetailResponse(
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
            serviceAreas,
            company.Staff.Count,
            company.CreatedAt);
    }
}
