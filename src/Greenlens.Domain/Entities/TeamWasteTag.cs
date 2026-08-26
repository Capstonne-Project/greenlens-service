namespace Greenlens.Domain.Entities;

/// <summary>
/// Join entity linking a Cleanup team to one or more WasteTags (team specialization).
/// </summary>
/// <remarks>Implements: BR-CLN-005.</remarks>
public sealed class TeamWasteTag
{
    private TeamWasteTag() { }

    public Guid TeamId { get; private set; }
    public Guid WasteTagId { get; private set; }

    public EnvironmentalTeam? Team { get; private set; }
    public WasteTag? WasteTag { get; private set; }

    public static TeamWasteTag Create(Guid teamId, Guid wasteTagId) =>
        new() { TeamId = teamId, WasteTagId = wasteTagId };
}
