namespace Greenlens.Domain.Enums;

/// <summary>
/// System roles per BR-ADM-002 (v1.1): 6 internal roles.
/// DEO = Department Environmental Officer (cấp Tỉnh/TP).
/// LEO = Local Environmental Officer (cấp Xã/Phường).
/// </summary>
public enum UserRole
{
    Citizen,
    DEO,
    LEO,
    Cleanup,
    Inspector,
    Admin
}
