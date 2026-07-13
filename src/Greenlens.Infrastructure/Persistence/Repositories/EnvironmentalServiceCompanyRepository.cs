using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class EnvironmentalServiceCompanyRepository(ApplicationDbContext db)
    : GenericRepository<EnvironmentalServiceCompany>(db), IEnvironmentalServiceCompanyRepository
{
    /// <inheritdoc />
    public async Task<bool> ServesWardAsync(Guid companyId, string wardCode, CancellationToken ct)
    {
        return await Context.CompanyServiceAreas
            .AnyAsync(sa => sa.CompanyId == companyId && sa.WardCode == wardCode, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<List<EnvironmentalServiceCompany>> GetBiddingExpiredAsync(DateTime asOfDate, CancellationToken ct)
        => Query()
            .Where(c => c.Status == CompanyStatus.Active
                && c.ContractType == ContractType.Bidding
                && c.ContractEndDate != null
                && c.ContractEndDate <= asOfDate)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<List<EnvironmentalServiceCompany>> GetBiddingExpiringBetweenAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct)
        => QueryAsNoTracking()
            .Where(c => c.Status == CompanyStatus.Active
                && c.ContractType == ContractType.Bidding
                && c.ContractEndDate != null
                && c.ContractEndDate >= fromDate
                && c.ContractEndDate <= toDate)
            .ToListAsync(ct);
}

