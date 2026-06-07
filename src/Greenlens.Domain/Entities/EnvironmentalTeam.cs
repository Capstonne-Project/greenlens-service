using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Environmental Team — either Cleanup or Inspection.
/// Dispatch is by NEED (v1.3), not by pollution type.
/// Cleanup: handles field cleanup for any pollution type.
/// Inspection: handles penalty enforcement — always ward-level (LEO-managed, BR-INS-001).
/// Community teams (CompanyId == null): managed by LEO, assigned directly.
/// Company teams (CompanyId != null): CRUD + assignment managed by CompanyManager.
///   LEO chỉ dispatch task sang company, CompanyManager phân công team.
///   InspectionTeam không thể thuộc company.
/// </summary>
/// <remarks>Implements: BR-ORG-003, BR-CLN-001, BR-INS-001.</remarks>
public sealed class EnvironmentalTeam : AuditableEntity
{
    private EnvironmentalTeam() { } // EF Core constructor

    public string Name { get; private set; } = default!;
    public Guid LocalOfficeId { get; private set; }
    public TeamType TeamType { get; private set; }
    public bool IsActive { get; private set; } = true;

    // ── Company affiliation (null = community team, set = company team) ──
    /// <summary>If set, this team belongs to an EnvironmentalServiceCompany. Null = community team (LEO-managed).</summary>
    public Guid? CompanyId { get; private set; }

    /// <summary>True if team belongs to a company (CompanyId != null).</summary>
    public bool IsCompanyTeam => CompanyId.HasValue;

    // ── Navigation ──
    public LocalOffice? LocalOffice { get; private set; }
    public EnvironmentalServiceCompany? Company { get; private set; }
    public ICollection<TeamMember> Members { get; private set; } = [];

    /// <summary>BR-ORG-003: Create a community team under a local office (LEO-managed).</summary>
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

    /// <summary>BR-CMP-004: Create a company team (CRUD by CompanyManager). InspectionTeam not allowed.</summary>
    public static EnvironmentalTeam CreateCompanyTeam(
        string name, Guid localOfficeId, TeamType teamType, Guid companyId)
    {
        if (teamType == TeamType.Inspection)
            throw new InvalidOperationException("InspectionTeam là đội xử phạt phường/xã, không thể thuộc công ty.");

        return new EnvironmentalTeam
        {
            Name = name,
            LocalOfficeId = localOfficeId,
            TeamType = teamType,
            CompanyId = companyId,
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
