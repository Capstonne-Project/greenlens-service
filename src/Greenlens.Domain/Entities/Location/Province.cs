namespace Greenlens.Domain.Entities.Location;

/// <summary>
/// Tỉnh / Thành phố trực thuộc TW. 34 đơn vị sau cải cách hành chính 2025.
/// </summary>
/// <remarks>
/// <see cref="Code"/> là khoá chính 2 ký tự (vd. "01" = Hà Nội, "79" = TP Hồ Chí Minh).
/// Được seed cố định từ <c>seed_data.sql</c>.
/// </remarks>
public sealed class Province
{
    public string Code { get; private set; } = string.Empty;       // PK, 2 chars
    public string Name { get; private set; } = string.Empty;

    public int AdministrativeRegionId { get; private set; }
    public AdministrativeRegion? AdministrativeRegion { get; private set; }

    public int AdministrativeUnitId { get; private set; }
    public AdministrativeUnit? AdministrativeUnit { get; private set; }

    /// <summary>URL tới GeoJSON polygon ranh giới tỉnh (CDN). Null nếu chưa có.</summary>
    public string? BoundaryUrl { get; private set; }

    public ICollection<Ward> Wards { get; private set; } = new List<Ward>();

    private Province() { } // EF

    public static Province Seed(string code, string name, int administrativeRegionId, int administrativeUnitId) => new()
    {
        Code = code,
        Name = name,
        AdministrativeRegionId = administrativeRegionId,
        AdministrativeUnitId = administrativeUnitId
    };

    internal void SetBoundaryUrl(string url) => BoundaryUrl = url;
}
