using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Organization.GetContractHistory;

/// <summary>
/// BR-CMP-006: Trả về lịch sử kỳ hợp đồng.
/// BR-CMP-021: CM chỉ xem company mình.
/// </summary>
public sealed class GetContractHistoryQueryHandler(
    IEnvironmentalServiceCompanyRepository companies,
    IUserRepository users,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser)
    : IRequestHandler<GetContractHistoryQuery, Result<ContractHistoryResponse>>
{
    public async Task<Result<ContractHistoryResponse>> Handle(
        GetContractHistoryQuery request,
        CancellationToken cancellationToken)
    {
        // BR-CMP-021: CM can only view own company
        var companyId = request.CompanyId;

        if (currentUser.Role == "CompanyManager")
        {
            var staff = await companyStaff
                .GetByUserIdAsync(currentUser.UserId, cancellationToken)
                .ConfigureAwait(false);

            if (staff is null)
                return Errors.Organization.NotCompanyManager;

            // Guid.Empty = CM endpoint (auto-resolve), otherwise cross-check
            if (companyId == Guid.Empty)
                companyId = staff.CompanyId;
            else if (staff.CompanyId != companyId)
                return Errors.Organization.CrossCompanyAccess;
        }

        var company = await companies.QueryAsNoTracking()
            .Include(c => c.ContractPeriods)
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            .ConfigureAwait(false);

        if (company is null)
            return Errors.Organization.CompanyNotFound;

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

        return new ContractHistoryResponse(
            company.Id,
            company.Name,
            periods);
    }
}
