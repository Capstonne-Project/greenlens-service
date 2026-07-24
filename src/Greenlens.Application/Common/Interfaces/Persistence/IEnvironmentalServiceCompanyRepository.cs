using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IEnvironmentalServiceCompanyRepository : IGenericRepository<EnvironmentalServiceCompany>
{
    /// <summary>
    /// Check contract number uniqueness including soft-deleted rows.
    /// Required because the DB unique index is not filtered by DeletedAt.
    /// </summary>
    Task<bool> ContractNumberExistsAsync(
        string contractNumber,
        Guid? excludeCompanyId = null,
        CancellationToken ct = default);

    /// <summary>Check if a company has the given ward in its service area.</summary>
    Task<bool> ServesWardAsync(Guid companyId, string wardCode, CancellationToken ct);

    /// <summary>BR-CMP-007: Get active Bidding companies with expired contracts.</summary>
    Task<List<EnvironmentalServiceCompany>> GetBiddingExpiredAsync(DateTime asOfDate, CancellationToken ct);

    /// <summary>BR-CMP-007: Get active Bidding companies expiring within a day range (inclusive).</summary>
    Task<List<EnvironmentalServiceCompany>> GetBiddingExpiringBetweenAsync(
        DateTime fromDate, DateTime toDate, CancellationToken ct);
}
