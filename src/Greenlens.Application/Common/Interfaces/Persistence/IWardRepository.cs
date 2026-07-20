namespace Greenlens.Application.Common.Interfaces.Persistence;

using Greenlens.Domain.Entities.Location;

public interface IWardRepository : ICatalogRepository<Ward>
{
    /// <summary>Simple lookup by code (no eager loading).</summary>
    Task<Ward?> GetByCodeAsync(string code, CancellationToken ct);

    /// <summary>
    /// Lấy tất cả ward thuộc 1 province (no-tracking, dùng cho dropdown phụ thuộc).
    /// </summary>
    Task<IReadOnlyList<Ward>> GetByProvinceAsync(string provinceCode, CancellationToken ct);

    Task<Ward?> GetByCodeWithCatalogAsync(string code, CancellationToken ct);
}
