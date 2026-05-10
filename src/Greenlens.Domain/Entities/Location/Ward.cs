namespace Greenlens.Domain.Entities.Location;

/// <summary>
/// Phường / Xã / Đặc khu (cấp đơn vị hành chính cơ sở sau cải cách 2025).
/// </summary>
/// <remarks>
/// <see cref="Code"/> là khoá chính 5 ký tự. ~3300 wards trên toàn quốc.
/// </remarks>
public sealed class Ward
{
    public string Code { get; private set; } = string.Empty;       // PK, 5 chars
    public string Name { get; private set; } = string.Empty;

    public string ProvinceCode { get; private set; } = string.Empty;
    public Province? Province { get; private set; }

    public int AdministrativeUnitId { get; private set; }
    public AdministrativeUnit? AdministrativeUnit { get; private set; }

    /// <summary>URL tới GeoJSON polygon ranh giới ward.</summary>
    public string? BoundaryUrl { get; private set; }

    private Ward() { } // EF

    public static Ward Seed(string code, string name, string provinceCode, int administrativeUnitId) => new()
    {
        Code = code,
        Name = name,
        ProvinceCode = provinceCode,
        AdministrativeUnitId = administrativeUnitId
    };

    internal void SetBoundaryUrl(string url) => BoundaryUrl = url;
}
