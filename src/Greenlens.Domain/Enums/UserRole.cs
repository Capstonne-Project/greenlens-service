namespace Greenlens.Domain.Enums;

/// <summary>
/// System roles per OVERVIEW v1.3: 8 human roles.
/// DEO = Department Environmental Officer — quản lý cấp Tỉnh: onboarding LEO, xuất data, cấu hình danh mục.
/// LEO = Local Environmental Officer — xác minh báo cáo, điều phối dọn dẹp &amp; xử phạt cấp Phường/Xã.
/// Cleaner = Thành viên đội dọn dẹp cộng đồng (CleanupTeam) cấp xã/phường — check-in, upload ảnh, đóng task.
/// CompanyManager = Quản lý Công ty Dịch vụ Môi trường (nhận task từ LEO, phân công Company Staff).
/// CompanyStaff = Nhân viên hiện trường thuộc công ty (check-in, upload ảnh, đóng task — luồng giống Cleaner).
/// Inspector = Thành viên Inspection Team, xử phạt cho mọi loại ô nhiễm.
/// </summary>
public enum UserRole
{
    Citizen,
    DEO,
    LEO,
    Cleaner,
    CompanyManager,
    CompanyStaff,
    Inspector,
    Admin
}
