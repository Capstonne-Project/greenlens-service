namespace Greenlens.Domain.Enums;

/// <summary>
/// Type of violating entity (cá nhân/hộ gia đình hoặc doanh nghiệp).
/// </summary>
/// <remarks>Implements: BR-INS-010.</remarks>
public enum ViolatorType
{
    /// <summary>Cá nhân hoặc hộ gia đình (identify bằng CMND/CCCD).</summary>
    Individual,

    /// <summary>Doanh nghiệp / cơ sở kinh doanh (identify bằng MST/MSDN).</summary>
    Business
}
