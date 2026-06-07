using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// CompanyStaff — nhân viên hiện trường thuộc Công ty Dịch vụ Môi trường.
/// Linked to a User with role CompanyStaff and an EnvironmentalServiceCompany.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class CompanyStaff : AuditableEntity
{
    private CompanyStaff() { } // EF Core constructor

    public Guid UserId { get; private set; }
    public Guid CompanyId { get; private set; }
    public string? Position { get; private set; }
    public bool IsActive { get; private set; } = true;

    // ── Navigation ──
    public User? User { get; private set; }
    public EnvironmentalServiceCompany? Company { get; private set; }

    /// <summary>BR-CMP-004: Company Manager adds staff member.</summary>
    public static CompanyStaff Create(Guid userId, Guid companyId, string? position = null)
    {
        return new CompanyStaff
        {
            UserId = userId,
            CompanyId = companyId,
            Position = position,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
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

    public void UpdatePosition(string? position)
    {
        Position = position;
        UpdatedAt = DateTime.UtcNow;
    }
}
