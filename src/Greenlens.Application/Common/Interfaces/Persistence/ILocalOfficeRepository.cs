using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface ILocalOfficeRepository : IGenericRepository<LocalOffice>
{
    Task<bool> ExistsByWardCodeAsync(string wardCode, CancellationToken ct = default);

    /// <summary>AND-filter by search tokens on office / ward / officer name (PostgreSQL ILike).</summary>
    IQueryable<LocalOffice> ApplySearchTokens(IQueryable<LocalOffice> query, IReadOnlyList<string> tokens);
}
