namespace Greenlens.Domain.Enums;

public enum MediaType
{
    Image,
    Video,
    Before,
    Progress,
    After,

    /// <summary>Ảnh hiện trường do Inspection Team chụp (BR-INS-010).</summary>
    Inspection,

    /// <summary>Evidence uploaded by citizen when requesting reopen (BR-REP-015).</summary>
    ReopenEvidence
}
