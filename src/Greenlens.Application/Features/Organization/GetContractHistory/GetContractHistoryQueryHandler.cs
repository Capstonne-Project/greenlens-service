using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.GetContractHistory;

/// <summary>
/// BR-CMP-006: Trả về lịch sử kỳ hợp đồng.
/// BR-CMP-021: CM chỉ xem company mình.
/// </summary>
public sealed class GetContractHistoryQueryHandler(
    IEnvironmentalServiceCompanyRepository companies,
    IUserRepository users,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    ILogger<GetContractHistoryQueryHandler> logger)
    : IRequestHandler<GetContractHistoryQuery, Result<ContractHistoryResponse>>
{
    public async Task<Result<ContractHistoryResponse>> Handle(
        GetContractHistoryQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting contract history for user {UserId}", currentUser.UserId);

        // BR-CMP-021: CM can only view own company
        var companyId = request.CompanyId;

        if (currentUser.Role == "CompanyManager")
        {
            var staff = await companyStaff
                .GetByUserIdAsync(currentUser.UserId, cancellationToken)
                .ConfigureAwait(false);

            if (staff is null || !staff.IsActive)
            {
                logger.LogWarning("Company manager not found or inactive for user ID {UserId}", currentUser.UserId);
                return Errors.Organization.NotCompanyManager;
            }

            // Guid.Empty = CM endpoint (auto-resolve), otherwise cross-check
            if (companyId == Guid.Empty)
                companyId = staff.CompanyId;
            else if (staff.CompanyId != companyId)
            {
                logger.LogWarning("Company ID {CompanyId} does not match user's company ID {CompanyId}", companyId, staff.CompanyId);
                return Errors.Organization.CrossCompanyAccess;
            }
        }

        var company = await companies.QueryAsNoTracking()
            .Include(c => c.ContractPeriods)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            .ConfigureAwait(false);

        if (company is null)
        {
            logger.LogWarning("Company {CompanyId} not found", companyId);
            return Errors.Organization.CompanyNotFound;
        }

        if (currentUser.Role == UserRole.DEO.ToString())
        {
            var deo = await users.GetByIdAsync(currentUser.UserId, cancellationToken).ConfigureAwait(false);
            if (deo is null)
            {
                logger.LogWarning("User not found for contract history: {UserId}", currentUser.UserId);
                return Errors.Users.UserNotFound;
            }

            var accessError = CompanyAccessAuthorization.ValidateViewAccess(company, deo);
            if (accessError is not null)
            {
                logger.LogWarning(
                    "User {UserId} denied contract history for company {CompanyId}: {ErrorCode}",
                    currentUser.UserId, companyId, accessError.Code);
                return accessError;
            }
        }

        // Resolve user names for each period
        var renewerIds = company.ContractPeriods
            .Select(p => p.RenewedByUserId)
            .Distinct()
            .ToList();

        var renewerNames = await users.QueryAsNoTracking()
            .Where(u => renewerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken)
            .ConfigureAwait(false);

        var periods = company.ContractPeriods
            .OrderByDescending(p => p.StartDate)
            .Select(p => new ContractPeriodDto(
                p.Id,
                p.ContractNumber,
                p.ContractType.ToString(),
                p.StartDate,
                p.EndDate,
                p.RenewedByUserId,
                renewerNames.GetValueOrDefault(p.RenewedByUserId),
                p.Note,
                p.CreatedAt))
            .ToList();

        logger.LogInformation("Contract history found: {CompanyId}, {Name}, {Periods}", company.Id, company.Name, periods.Count);

        return new ContractHistoryResponse(
            company.Id,
            company.Name,
            periods);
    }
}
