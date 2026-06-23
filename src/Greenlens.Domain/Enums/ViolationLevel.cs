namespace Greenlens.Domain.Enums;

/// <summary>
/// Severity level of an environmental violation (BR-INS-011).
/// Admin configures penalty amount ranges per level (BR-ADM-008).
/// </summary>
public enum ViolationLevel
{
    /// <summary>Nhẹ — cảnh cáo.</summary>
    Minor,

    /// <summary>Trung bình.</summary>
    Moderate,

    /// <summary>Nặng.</summary>
    Severe,

    /// <summary>Đặc biệt nghiêm trọng.</summary>
    Critical
}
