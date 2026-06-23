namespace Greenlens.Domain.Enums;

/// <summary>
/// Type of environmental team.
/// Dispatch is by NEED (v1.3), not by pollution type.
/// Cleanup: handles field cleanup (any pollution type).
/// Inspection: handles penalty enforcement for ALL pollution types (BR-INS-001).
/// </summary>
/// <remarks>Implements: BR-ORG-013, BR-CLN-001, BR-INS-001.</remarks>
public enum TeamType
{
    Cleanup,
    Inspection
}
