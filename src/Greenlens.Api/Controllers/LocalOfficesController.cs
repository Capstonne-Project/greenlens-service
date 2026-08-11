using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.AssignLeoToOffice;
using Greenlens.Application.Features.Organization.CreateLocalOffice;
using Greenlens.Application.Features.Organization.GetLocalOfficeById;
using Greenlens.Application.Features.Organization.GetLocalOffices;
using Greenlens.Application.Features.Organization.GetMyWardBoundary;
using Greenlens.Application.Features.Organization.GetOfficeStaff;
using Greenlens.Application.Features.Organization.LookupCitizenByEmail;
using Greenlens.Application.Features.Organization.RecruitStaff;
using Greenlens.Application.Features.Organization.ReleaseStaff;
using Greenlens.Application.Features.Organization.UpdateLocalOffice;
using Greenlens.Application.Features.Reports.GetOfficeReports;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Quản lý Local Office (Văn phòng MT cấp Xã/Phường).</summary>
[ApiController]
[Route("v1/offices")]
[Authorize]
[Produces("application/json")]
public sealed class LocalOfficesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,DEO,LEO")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(Summary = "[Admin/DEO/LEO] Danh sách offices", Description = "Trả về danh sách văn phòng MT cấp xã/phường. Hỗ trợ lọc theo department và trạng thái.")]
    [SwaggerResponse(200, "Danh sách offices", typeof(ApiResponse<GetLocalOfficesResponse>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? departmentId = null, [FromQuery] bool? isOnboarded = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetLocalOfficesQuery(page, pageSize, departmentId, isOnboarded), ct)).ToHttp();

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,DEO,LEO")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(Summary = "[Admin/DEO/LEO] Chi tiết office", Description = "Trả về thông tin office kèm danh sách teams trực thuộc, thông tin officer phụ trách.")]
    [SwaggerResponse(200, "Chi tiết office", typeof(ApiResponse<LocalOfficeDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetLocalOfficeByIdQuery(id), ct)).ToHttp();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(Summary = "[Admin] Tạo office", Description = "Onboard văn phòng MT cấp xã/phường. Sau khi tạo, báo cáo trong ward đó sẽ tự động route đến office này.")]
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<CreateLocalOfficeResponse>))]
    [SwaggerResponse(409, "Ward đã có office", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateLocalOfficeCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp("Đã tạo văn phòng thành công.");

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật office", Description = "Cập nhật tên văn phòng.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid id, [FromBody] UpdateLocalOfficeRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateLocalOfficeCommand(id, request.Name), ct)).ToHttpNoContent("Đã cập nhật văn phòng.");

    [HttpPut("{id:guid}/officer")]
    [Authorize(Roles = "Admin")]
    [Tags("⚙️ Admin Dashboard")]
    [SwaggerOperation(Summary = "[Admin] Gán LEO cho office", Description = "Gán 1 user có role LEO làm người phụ trách văn phòng.")]
    [SwaggerResponse(200, "Đã gán", typeof(ApiResponse))]
    [SwaggerResponse(404, "Office hoặc User không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "User không có role LEO", typeof(ApiResponse))]
    public async Task<IActionResult> AssignOfficerAsync(
        [FromRoute] Guid id, [FromBody] AssignLeoRequest request, CancellationToken ct)
        => (await sender.Send(new AssignLeoToOfficeCommand(id, request.UserId), ct)).ToHttpNoContent("Đã gán LEO cho văn phòng.");

    /// <summary>Tất cả báo cáo thuộc office mà LEO quản lý, kèm tiến độ của team.</summary>
    [HttpGet("my/reports")]
    [Authorize(Roles = "LEO")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Danh sách tất cả báo cáo trong office (phân trang, kèm tiến độ team)",
        Description =
            "Trả về tất cả báo cáo thuộc LocalOffice mà LEO đang quản lý. " +
            "Mỗi báo cáo kèm danh sách team đã được gán (assignment progress): " +
            "tên team, loại team, trạng thái assignment, phần trăm tiến độ, ghi chú, thời gian. " +
            "Mỗi item có `thumbnails` (mảng URL ảnh đầu tiên của báo cáo, MediaType=Image). " +
            "Hỗ trợ tìm kiếm (mã báo cáo, mô tả, địa chỉ), lọc theo status/category/severity/assignmentStatus, " +
            "khoảng ngày tạo (fromDate, toDate — ISO date, inclusive theo ngày UTC), " +
            "sắp xếp theo: code, status, severity, priority, createdAt, assignmentCount (mặc định: mới nhất).")]
    [SwaggerResponse(200, "Danh sách báo cáo kèm tiến độ", typeof(ApiResponse<GetOfficeReportsResponse>))]
    [SwaggerResponse(404, "Chưa gán local office", typeof(ApiResponse))]
    public async Task<IActionResult> GetOfficeReportsAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] ReportStatus? status = null,
        [FromQuery] Guid? categoryId = null, [FromQuery] Severity? severity = null,
        [FromQuery] AssignmentStatus? assignmentStatus = null,
        [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
        => (await sender.Send(new GetOfficeReportsQuery(
            page, pageSize, search, status, categoryId, severity, assignmentStatus,
            fromDate, toDate, sortBy, sortDesc), ct)).ToHttp();

    /// <summary>Ranh giới (boundary) phường mà LEO đang quản lý, suy trực tiếp từ JWT.</summary>
    [HttpGet("my/ward-boundary")]
    [Authorize(Roles = "LEO")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Ranh giới phường của office mình",
        Description =
            "Trả về wardCode/wardName/boundaryUrl của LocalOffice mà LEO đang quản lý — suy từ JWT, " +
            "không cần truyền tham số. boundaryUrl là link GeoJSON (Polygon/MultiPolygon) để FE vẽ " +
            "mask/fit-bounds trên bản đồ officer/map; có thể null nếu phường chưa có dữ liệu ranh giới.")]
    [SwaggerResponse(200, "Ranh giới phường", typeof(ApiResponse<GetMyWardBoundaryResponse>))]
    [SwaggerResponse(404, "Chưa gán local office", typeof(ApiResponse))]
    public async Task<IActionResult> GetMyWardBoundaryAsync(CancellationToken ct)
        => (await sender.Send(new GetMyWardBoundaryQuery(), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  STAFF MANAGEMENT
    // ═══════════════════════════════════════════

    [HttpGet("my/staff/lookup")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Tra cứu tài khoản theo email",
        Description = "Tra cứu thông tin tài khoản Citizen theo email chính xác. " +
            "Trả về thông tin cơ bản + trạng thái đủ điều kiện recruit (isRecruitEligible). " +
            "Dùng để FE hiển preview trước khi LEO bấm Tuyển.")]
    [SwaggerResponse(200, "Thông tin tài khoản", typeof(ApiResponse<CitizenLookupResponse>))]
    [SwaggerResponse(404, "Không tìm thấy email", typeof(ApiResponse))]
    public async Task<IActionResult> LookupCitizenAsync(
        [FromQuery] string email, CancellationToken ct)
        => (await sender.Send(new LookupCitizenByEmailQuery(email), ct)).ToHttp();

    [HttpPost("my/staff")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Gửi lời mời tuyển nhân sự (BR-ORG-020/021)",
        Description = "Search Citizen theo email → tạo StaffInvitation (7 ngày). " +
            "Citizen phải Accept trước khi role đổi. " +
            "Nếu truyền teamId → sẽ thêm vào team khi Citizen accept. " +
            "Chỉ Citizen mới được mời. User đã thuộc phường khác → reject.")]
    [SwaggerResponse(200, "Đã gửi lời mời", typeof(ApiResponse<RecruitStaffResponse>))]
    [SwaggerResponse(404, "Không tìm thấy email trong hệ thống", typeof(ApiResponse))]
    [SwaggerResponse(409, "User đã thuộc phường/team khác hoặc đã có invitation pending", typeof(ApiResponse))]
    [SwaggerResponse(422, "Role không hợp lệ hoặc chưa gán office", typeof(ApiResponse))]
    public async Task<IActionResult> RecruitStaffAsync(
        [FromBody] RecruitStaffRequest request, CancellationToken ct)
        => (await sender.Send(
            new RecruitStaffCommand(request.Email, request.TargetRole, request.TeamId, request.IsLeader), ct))
            .ToHttp("Đã gửi lời mời thành công.");

    [HttpDelete("my/staff/{userId:guid}")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Release nhân sự về Citizen",
        Description = "Gỡ Cleaner/Inspector khỏi phường: revert role về Citizen, xoá khỏi tất cả team, " +
            "clear LocalOfficeId. Dùng khi LEO add nhầm hoặc nhân sự nghỉ.")]
    [SwaggerResponse(200, "Đã release nhân sự", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy user", typeof(ApiResponse))]
    [SwaggerResponse(403, "User không thuộc phường của bạn", typeof(ApiResponse))]
    public async Task<IActionResult> ReleaseStaffAsync(
        Guid userId, CancellationToken ct)
        => (await sender.Send(new ReleaseStaffCommand(userId), ct)).ToHttpNoContent();

    [HttpGet("my/staff")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Danh sách nhân sự trong phường",
        Description = "Trả về danh sách Cleaner/Inspector thuộc LocalOffice mà LEO đang quản lý. " +
            "Bao gồm thông tin team (nếu có). " +
            "Hỗ trợ tìm kiếm (tên, email), lọc theo role và trạng thái team (hasTeam=true/false).")]
    [SwaggerResponse(200, "Danh sách nhân sự", typeof(ApiResponse<GetOfficeStaffResponse>))]
    [SwaggerResponse(422, "Chưa gán office", typeof(ApiResponse))]
    public async Task<IActionResult> GetOfficeStaffAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? hasTeam = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetOfficeStaffQuery(page, pageSize, search, role, hasTeam), ct)).ToHttp();
}

public sealed record UpdateLocalOfficeRequest(string Name);
public sealed record AssignLeoRequest(Guid UserId);
public sealed record RecruitStaffRequest(string Email, UserRole TargetRole, Guid? TeamId = null, bool IsLeader = false);

