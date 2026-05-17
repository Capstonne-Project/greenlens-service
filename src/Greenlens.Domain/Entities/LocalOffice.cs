using Greenlens.Domain.Common;
using Greenlens.Domain.Entities.Location;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Local Environmental Office — cấp Xã/Phường. Trực thuộc Department.
/// Mỗi xã/phường được phân công 1 LEO + N Cleanup Team + N Inspection Team.
/// </summary>
/// <remarks>Implements: BR-ORG-002, BR-ORG-004.</remarks>
public sealed class LocalOffice : AuditableEntity
{
    private LocalOffice() { } // EF Core constructor

    public string Name { get; private set; } = default!;
    public Guid DepartmentId { get; private set; }
    public string WardCode { get; private set; } = default!;
    public Guid? OfficerId { get; private set; }
    public bool IsOnboarded { get; private set; } = true;

    // ── Navigation ──
    public Department? Department { get; private set; }
    public Ward? Ward { get; private set; }
    public User? Officer { get; private set; }
    public ICollection<EnvironmentalTeam> Teams { get; private set; } = [];

    /// <summary>BR-ORG-002, BR-ADM-011: Onboard a new ward/commune office.</summary>
    public static LocalOffice Create(string name, Guid departmentId, string wardCode, Guid? officerId = null)
    {
        return new LocalOffice
        {
            Name = name,
            DepartmentId = departmentId,
            WardCode = wardCode,
            OfficerId = officerId,
            IsOnboarded = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>BR-ORG-002: Assign or replace the LEO for this office.</summary>
    public void AssignOfficer(Guid officerId)
    {
        OfficerId = officerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveOfficer()
    {
        OfficerId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsOnboarded = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
