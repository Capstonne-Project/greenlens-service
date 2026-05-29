namespace Greenlens.Domain.Entities;

/// <summary>
/// Join entity linking a Report to one or more WasteTags.
/// Officers tag reports during verification or assignment so cleanup teams
/// know the exact types of waste at the site.
/// </summary>
public sealed class ReportWasteTag
{
    private ReportWasteTag() { } // EF Core constructor

    public Guid ReportId { get; private set; }
    public Guid WasteTagId { get; private set; }
    public Guid TaggedById { get; private set; }
    public DateTime TaggedAt { get; private set; }

    // ── Navigation ──
    public Report? Report { get; private set; }
    public WasteTag? WasteTag { get; private set; }
    public User? TaggedByUser { get; private set; }

    public static ReportWasteTag Create(Guid reportId, Guid wasteTagId, Guid taggedById)
    {
        return new ReportWasteTag
        {
            ReportId = reportId,
            WasteTagId = wasteTagId,
            TaggedById = taggedById,
            TaggedAt = DateTime.UtcNow
        };
    }
}
