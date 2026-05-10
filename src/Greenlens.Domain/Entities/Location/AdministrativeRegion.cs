namespace Greenlens.Domain.Entities.Location;

/// <summary>
/// Vùng địa lý hành chính của Việt Nam (8 vùng cố định).
/// VD: Đông Bắc Bộ, Tây Bắc Bộ, Đồng bằng sông Hồng, ...
/// </summary>
/// <remarks>
/// ID 1-8 là cố định, được seed từ <c>seed_data.sql</c>. Không cho phép tạo mới ở runtime.
/// </remarks>
public sealed class AdministrativeRegion
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public ICollection<Province> Provinces { get; private set; } = new List<Province>();

    private AdministrativeRegion() { } // EF

    public static AdministrativeRegion Seed(int id, string name) => new()
    {
        Id = id,
        Name = name
    };
}
