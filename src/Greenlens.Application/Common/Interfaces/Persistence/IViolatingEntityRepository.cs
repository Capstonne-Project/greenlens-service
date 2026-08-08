using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IViolatingEntityRepository : IGenericRepository<ViolatingEntity>
{
    /// <summary>Find by TaxCode (MST/MSDN) — for Business type.</summary>
    Task<ViolatingEntity?> FindByTaxCodeAsync(string taxCode, CancellationToken ct = default);

    /// <summary>Find by IdentityNumber (CMND/CCCD) — for Individual type.</summary>
    Task<ViolatingEntity?> FindByIdentityNumberAsync(string identityNumber, CancellationToken ct = default);

    /// <summary>Check tax code uniqueness including soft-deleted rows.</summary>
    Task<bool> TaxCodeExistsAsync(string taxCode, Guid? excludeEntityId = null, CancellationToken ct = default);

    /// <summary>Check identity number uniqueness among active rows (non-unique index; used for dedup).</summary>
    Task<bool> IdentityNumberExistsAsync(string identityNumber, Guid? excludeEntityId = null, CancellationToken ct = default);

    /// <summary>Search by name (partial match).</summary>
    Task<List<ViolatingEntity>> SearchByNameAsync(string name, int maxResults = 20, CancellationToken ct = default);

    /// <summary>BR-INS-022: Count inspection reports for this violating entity within a period.</summary>
    Task<int> CountInspectionsInPeriodAsync(Guid violatingEntityId, int months, CancellationToken ct = default);

    /// <summary>True if any inspection report references this violating entity.</summary>
    Task<bool> HasAnyInspectionReportsAsync(Guid violatingEntityId, CancellationToken ct = default);
}
