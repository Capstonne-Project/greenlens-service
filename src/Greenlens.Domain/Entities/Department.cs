using Greenlens.Domain.Common;
using Greenlens.Domain.Entities.Location;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Department of Environmental Management — cấp Tỉnh/Thành phố.
/// Đơn vị quản lý cấp cao nhất. Mỗi tỉnh/thành phố có 1 Department.
/// </summary>
/// <remarks>Implements: BR-ORG-001.</remarks>
public sealed class Department : AuditableEntity
{
    private Department() { } // EF Core constructor

    public string Name { get; private set; } = default!;
    public string ProvinceCode { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    // ── Navigation ──
    public Province? Province { get; private set; }
    public ICollection<LocalOffice> LocalOffices { get; private set; } = [];

    /// <summary>BR-ORG-001: Create a Department for a Province.</summary>
    public static Department Create(string name, string provinceCode)
    {
        return new Department
        {
            Name = name,
            ProvinceCode = provinceCode,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
