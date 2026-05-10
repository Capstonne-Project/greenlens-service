namespace Greenlens.Application.Common.Interfaces.Persistence;

using Greenlens.Domain.Entities.Location;

/// <summary>
/// Read-only repository cho danh mục vùng hành chính. Body rỗng — base contract đủ dùng.
/// Dữ liệu được seed cố định, Application chỉ đọc.
/// </summary>
public interface IAdministrativeRegionRepository : ICatalogRepository<AdministrativeRegion>;

/// <summary>
/// Read-only repository cho danh mục loại đơn vị hành chính. Body rỗng — base contract đủ dùng.
/// </summary>
public interface IAdministrativeUnitRepository : ICatalogRepository<AdministrativeUnit>;
