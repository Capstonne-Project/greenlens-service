namespace Greenlens.Application.Common.Interfaces.Persistence;

using Greenlens.Domain.Entities.Location;

public interface IProvinceRepository : ICatalogRepository<Province>
{
    /// <summary>
    /// Tải tỉnh theo code, eager-load <see cref="Province.AdministrativeRegion"/> + <see cref="Province.AdministrativeUnit"/>.
    /// </summary>
    Task<Province?> GetByCodeWithCatalogAsync(string code, CancellationToken ct);

    /// <summary>
    /// Lấy danh sách 34 provinces dùng cho dropdown / map filter (no-tracking, projection-friendly).
    /// </summary>
    Task<IReadOnlyList<Province>> GetAllForListAsync(CancellationToken ct);
}
