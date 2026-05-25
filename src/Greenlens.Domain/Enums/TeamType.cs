namespace Greenlens.Domain.Enums;

/// <summary>
/// Type of environmental team.
/// Cleanup handles: Rác thải, Nước thải, Hóa chất.
/// Inspection handles: Tiếng ồn, Không khí (xử phạt).
/// </summary>
/// <remarks>Implements: BR-ORG-013, BR-CLN-001, BR-INS-001.</remarks>
public enum TeamType
{
    Cleanup,
    Inspection
}
