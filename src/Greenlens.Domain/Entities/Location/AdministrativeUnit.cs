namespace Greenlens.Domain.Entities.Location;

/// <summary>
/// Loại đơn vị hành chính: Thành phố trực thuộc TW, Tỉnh, Phường, Xã, Đặc khu (5 loại cố định).
/// </summary>
/// <remarks>
/// ID 1-5 được seed từ <c>seed_data.sql</c>. Phân biệt giữa Province (id 1-2) và Ward (id 3-5).
/// </remarks>
public sealed class AdministrativeUnit
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;          // "Tỉnh", "Thành phố trực thuộc trung ương", ...
    public string Abbreviation { get; private set; } = string.Empty;  // "Tỉnh", "Thành phố", "Phường", "Xã", "Đặc khu"

    public ICollection<Province> Provinces { get; private set; } = new List<Province>();
    public ICollection<Ward> Wards { get; private set; } = new List<Ward>();

    private AdministrativeUnit() { } // EF

    public static AdministrativeUnit Seed(int id, string name, string abbreviation) => new()
    {
        Id = id,
        Name = name,
        Abbreviation = abbreviation
    };
}
