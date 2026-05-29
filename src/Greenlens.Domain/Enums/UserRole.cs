namespace Greenlens.Domain.Enums;

/// <summary>
/// System roles per BR-ADM-002 (v2.0): 6 internal roles with two-tier dispatch model.
/// DEO = Department Environmental Officer — Điều phối viên cấp Tỉnh: tiếp nhận, xác minh, lập task, điều phối xuống phường/xã.
/// LEO = Local Environmental Officer — Giám sát viên cấp Phường/Xã: nhận task từ tỉnh, phân công và quản lý team.
/// </summary>
public enum UserRole
{
    Citizen,
    DEO,
    LEO,
    Cleaner,
    Inspector,
    Admin
}
