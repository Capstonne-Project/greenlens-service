using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Pollution category (TRASH, WASTEWATER, SMOKE, NOISE, CHEMICAL, OTHER).
/// Seeded at startup. Admin can toggle <see cref="IsActive"/>.
/// </summary>
/// <remarks>Implements: BR-REP-005.</remarks>
public sealed class PollutionCategory : BaseEntity
{
    private PollutionCategory() { }

    public string Code { get; private set; } = default!;
    public string NameVi { get; private set; } = default!;
    public string NameEn { get; private set; } = default!;
    public string? IconUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    // ── Navigation ──
    public ICollection<Report> Reports { get; private set; } = [];

    public static PollutionCategory Create(string code, string nameVi, string nameEn, string? iconUrl = null)
    {
        return new PollutionCategory
        {
            Code = code,
            NameVi = nameVi,
            NameEn = nameEn,
            IconUrl = iconUrl,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void Update(string nameVi, string nameEn, string? iconUrl)
    {
        NameVi = nameVi;
        NameEn = nameEn;
        IconUrl = iconUrl;
    }
}
