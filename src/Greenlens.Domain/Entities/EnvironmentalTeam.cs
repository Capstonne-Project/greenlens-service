using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Environmental Team — either Cleanup or Inspection.
/// Cleanup: handles Rác thải, Nước thải, Hóa chất (dọn dẹp).
/// Inspection: handles Tiếng ồn, Không khí (xử phạt).
/// </summary>
/// <remarks>Implements: BR-ORG-003, BR-CLN-001, BR-INS-001.</remarks>
public sealed class EnvironmentalTeam : AuditableEntity
{
    private EnvironmentalTeam() { } // EF Core constructor

    public string Name { get; private set; } = default!;
    public Guid LocalOfficeId { get; private set; }
    public TeamType TeamType { get; private set; }
    public bool IsActive { get; private set; } = true;

    // ── Navigation ──
    public LocalOffice? LocalOffice { get; private set; }
    public ICollection<TeamMember> Members { get; private set; } = [];

    /// <summary>BR-ORG-003: Create a new team under a local office.</summary>
    public static EnvironmentalTeam Create(string name, Guid localOfficeId, TeamType teamType)
    {
        return new EnvironmentalTeam
        {
            Name = name,
            LocalOfficeId = localOfficeId,
            TeamType = teamType,
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

    /// <summary>BR-ORG-003: Transfer team to another office (Admin only, with audit).</summary>
    public void TransferToOffice(Guid newOfficeId)
    {
        LocalOfficeId = newOfficeId;
        UpdatedAt = DateTime.UtcNow;
    }
}
