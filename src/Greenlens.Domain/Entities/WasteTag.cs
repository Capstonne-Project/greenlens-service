using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Lookup table for waste types found at pollution sites.
/// Officers tag reports with one or more WasteTags so cleanup teams
/// know what equipment and protective gear to bring.
/// </summary>
/// <remarks>Seeded at startup. Admin can toggle <see cref="IsActive"/>.</remarks>
public sealed class WasteTag : SoftDeletableEntity
{
    private WasteTag() { }

    public string Code { get; private set; } = default!;
    public string NameVi { get; private set; } = default!;
    public string NameEn { get; private set; } = default!;
    public string? IconUrl { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // ── Navigation ──
    public ICollection<ReportWasteTag> ReportWasteTags { get; private set; } = [];

    public static WasteTag Create(
        string code,
        string nameVi,
        string nameEn,
        string? iconUrl = null,
        string? description = null,
        int displayOrder = 0)
    {
        return new WasteTag
        {
            Code = code,
            NameVi = nameVi,
            NameEn = nameEn,
            IconUrl = iconUrl,
            Description = description,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string nameVi, string nameEn, string? iconUrl, string? description, int displayOrder)
    {
        NameVi = nameVi;
        NameEn = nameEn;
        IconUrl = iconUrl;
        Description = description;
        DisplayOrder = displayOrder;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
