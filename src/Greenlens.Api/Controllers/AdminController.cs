using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Admin.ArchiveCategory;
using Greenlens.Application.Features.Admin.CreateCategory;
using Greenlens.Application.Features.Admin.ForceUpdateReportStatus;
using Greenlens.Application.Features.Admin.GetAdminReports;
using Greenlens.Application.Features.Admin.UpdateCategory;
using Greenlens.Application.Features.Admin.UpdateUserRole;
using Greenlens.Application.Features.Reports.GetReportById;
using Greenlens.Application.Features.Users;
using Greenlens.Application.Features.Users.CreateAccount;
using Greenlens.Application.Features.Users.DeleteUser;
using Greenlens.Application.Features.Users.GetAllUsers;
using Greenlens.Application.Features.Users.GetAllUsersWithPaged;
using Greenlens.Application.Features.Users.GetUserById;
using Greenlens.Application.Features.Users.UpdateUser;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Admin Dashboard — Users, Reports, Categories, Roles.</summary>
[ApiController]
[Route("v1/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminController(ISender sender) : ControllerBase
{
    // ═══════════════════════════════════════════
    // ██  USERS
    // ═══════════════════════════════════════════

    [HttpPost("users")]
    [SwaggerOperation(Summary = "[Admin] Tạo tài khoản", Description = "Tạo tài khoản mới (Officer, Cleanup, Inspector, Citizen). Email tự động xác minh.")]
    [SwaggerResponse(201, "Đã tạo", typeof(ApiResponse<CreateAccountResponse>))]
    [SwaggerResponse(409, "Email đã tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAccountAsync(
        [FromBody] CreateAccountCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpGet("users/all")]
    [SwaggerOperation(Summary = "[Admin] Toàn bộ users (không phân trang)", Description = "Trả về danh sách tất cả user không phân trang. Dùng cho dropdown/autocomplete.")]
    [SwaggerResponse(200, "Danh sách user", typeof(ApiResponse<List<UserListItemDto>>))]
    public async Task<IActionResult> GetAllUsersAsync(CancellationToken ct)
        => (await sender.Send(new GetAllUsersQuery(), ct)).ToHttp();

    [HttpGet("users")]
    [SwaggerOperation(Summary = "[Admin] Danh sách users (phân trang)", Description = "Danh sách user có phân trang, tìm kiếm, lọc theo role và trạng thái email.")]
    [SwaggerResponse(200, "Danh sách user", typeof(ApiResponse<PagedList<UserListItemDto>>))]
    public async Task<IActionResult> GetAllUsersWithPagedAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null, [FromQuery] UserRole? role = null,
        [FromQuery] bool? isEmailVerified = null, CancellationToken ct = default)
        => (await sender.Send(
            new GetAllUsersWithPagedQuery(page, pageSize, search, role, isEmailVerified), ct)).ToHttp();

    [HttpGet("users/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Chi tiết user", Description = "Trả về thông tin chi tiết user theo ID.")]
    [SwaggerResponse(200, "Chi tiết user", typeof(ApiResponse<UserDetailDto>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetUserByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetUserByIdQuery(id), ct)).ToHttp();

    [HttpPut("users/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật user", Description = "Cập nhật tên, SĐT, role, trạng thái xác minh email.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse<UpdateUserResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateUserAsync(
        [FromRoute] Guid id, [FromBody] AdminUpdateUserRequest request, CancellationToken ct)
        => (await sender.Send(
            new UpdateUserCommand(id, request.FullName, request.PhoneNumber, request.Role, request.IsEmailVerified), ct)).ToHttp();

    [HttpDelete("users/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Xóa user (soft-delete)", Description = "Soft-delete user (IsDeleted = true). Không thể tự xóa chính mình.")]
    [SwaggerResponse(200, "Đã xóa", typeof(ApiResponse<DeleteUserResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    [SwaggerResponse(422, "Không thể xóa chính mình", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteUserAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new DeleteUserCommand(id), ct)).ToHttp();

    [HttpPut("users/{id:guid}/role")]
    [SwaggerOperation(Summary = "[Admin] Đổi role user", Description = "Thay đổi role của user. Dùng khi cần chuyển Citizen → LEO, hoặc LEO → DEO...")]
    [SwaggerResponse(204, "Đã đổi role")]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateUserRoleAsync(
        [FromRoute] Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateUserRoleCommand(id, request.NewRole), ct)).ToHttpNoContent();

    // ═══════════════════════════════════════════
    // ██  REPORTS
    // ═══════════════════════════════════════════

    [HttpGet("reports")]
    [SwaggerOperation(Summary = "[Admin] Danh sách báo cáo (admin view)", Description = "Danh sách báo cáo toàn hệ thống với đầy đủ metadata. Hỗ trợ search, filter theo status/category/ward/province.")]
    [SwaggerResponse(200, "Danh sách báo cáo", typeof(ApiResponse<GetAdminReportsResponse>))]
    public async Task<IActionResult> GetAdminReportsAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] ReportStatus? status = null, [FromQuery] Guid? categoryId = null,
        [FromQuery] string? wardCode = null, [FromQuery] string? provinceCode = null,
        [FromQuery] string? search = null, CancellationToken ct = default)
        => (await sender.Send(
            new GetAdminReportsQuery(page, pageSize, status, categoryId, wardCode, provinceCode, search), ct)).ToHttp();

    [HttpGet("reports/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Chi tiết báo cáo", Description = "Trả về full chi tiết báo cáo kèm media, assignments, history. Dùng chung với ReportsController.GetByIdAsync.")]
    [SwaggerResponse(200, "Chi tiết báo cáo", typeof(ApiResponse<ReportDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetAdminReportByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetReportByIdQuery(id), ct)).ToHttp();

    [HttpPut("reports/{id:guid}/status")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật status báo cáo", Description = "Admin override: chuyển status bất kỳ, bypass state machine. Cần lý do ≥ 10 ký tự.")]
    [SwaggerResponse(204, "Đã cập nhật status")]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> ForceUpdateStatusAsync(
        [FromRoute] Guid id, [FromBody] ForceUpdateStatusRequest request, CancellationToken ct)
        => (await sender.Send(
            new ForceUpdateReportStatusCommand(id, request.NewStatus, request.Reason), ct)).ToHttpNoContent();

    // ═══════════════════════════════════════════
    // ██  POLLUTION CATEGORIES
    // ═══════════════════════════════════════════

    [HttpPost("pollution-categories")]
    [SwaggerOperation(Summary = "[Admin] Tạo danh mục ô nhiễm", Description = "Tạo loại ô nhiễm mới (code, tên VN, tên EN, icon URL).")]
    [SwaggerResponse(201, "Đã tạo", typeof(ApiResponse<CreateCategoryResponse>))]
    public async Task<IActionResult> CreateCategoryAsync(
        [FromBody] CreateCategoryCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpPut("pollution-categories/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật danh mục ô nhiễm", Description = "Cập nhật tên VN, tên EN, icon URL của danh mục.")]
    [SwaggerResponse(204, "Đã cập nhật")]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateCategoryAsync(
        [FromRoute] Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
        => (await sender.Send(
            new UpdateCategoryCommand(id, request.NameVi, request.NameEn, request.IconUrl), ct)).ToHttpNoContent();

    [HttpDelete("pollution-categories/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Xóa danh mục (soft-delete)", Description = "Deactivate danh mục. Các báo cáo cũ vẫn giữ reference. Danh mục sẽ không xuất hiện cho citizen nữa.")]
    [SwaggerResponse(204, "Đã xóa")]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteCategoryAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new ArchiveCategoryCommand(id, Archive: true), ct)).ToHttpNoContent();

    [HttpPut("pollution-categories/{id:guid}/archive")]
    [SwaggerOperation(Summary = "[Admin] Archive/Unarchive danh mục", Description = "Toggle trạng thái active/inactive. Body: { archive: true/false }.")]
    [SwaggerResponse(204, "Đã cập nhật")]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> ArchiveCategoryAsync(
        [FromRoute] Guid id, [FromBody] ArchiveCategoryRequest request, CancellationToken ct)
        => (await sender.Send(new ArchiveCategoryCommand(id, request.Archive), ct)).ToHttpNoContent();

    // ═══════════════════════════════════════════
    // ██  ROLES & PERMISSIONS
    // ═══════════════════════════════════════════

    [HttpGet("roles")]
    [SwaggerOperation(Summary = "[Admin] Danh sách roles", Description = "Trả về danh sách role hệ thống (enum-based). Mỗi role kèm mô tả.")]
    [SwaggerResponse(200, "Danh sách roles", typeof(ApiResponse<List<RoleDto>>))]
    public Task<IActionResult> GetRolesAsync()
    {
        var roles = Enum.GetValues<UserRole>()
            .Select(r => new RoleDto(r.ToString(), GetRoleDescription(r)))
            .ToList();
        IActionResult result = Ok(new ApiResponse<List<RoleDto>> { Data = roles });
        return Task.FromResult(result);
    }

    [HttpGet("permissions")]
    [SwaggerOperation(Summary = "[Admin] Danh sách permissions theo role", Description = "Trả về ma trận phân quyền: mỗi role có danh sách endpoint patterns được truy cập.")]
    [SwaggerResponse(200, "Permission matrix", typeof(ApiResponse<List<RolePermissionDto>>))]
    public Task<IActionResult> GetPermissionsAsync()
    {
        var matrix = new List<RolePermissionDto>
        {
            new("Admin", ["*"]),
            new("DEO", ["GET /departments", "GET /offices", "GET /teams", "GET /reports", "PUT /reports/*/verify", "PUT /reports/*/reject", "POST /reports/*/assign", "PUT /reports/*/reassign", "GET /reports/queue"]),
            new("LEO", ["GET /offices", "GET /teams", "GET /reports", "PUT /reports/*/verify", "PUT /reports/*/reject", "POST /reports/*/assign", "PUT /reports/*/reassign", "GET /reports/queue"]),
            new("Cleanup", ["PUT /reports/*/resolve", "PUT /reports/*/decline"]),
            new("Inspector", ["PUT /reports/*/penalty", "PUT /reports/*/close-no-violation", "PUT /reports/*/decline"]),
            new("Citizen", ["POST /reports", "GET /reports/my", "PUT /reports/*/close", "PUT /reports/*/reopen"])
        };
        IActionResult result = Ok(new ApiResponse<List<RolePermissionDto>> { Data = matrix });
        return Task.FromResult(result);
    }

    // ── Helpers ──

    private static string GetRoleDescription(UserRole role) => role switch
    {
        UserRole.Citizen => "Người dân — tạo và theo dõi báo cáo ô nhiễm",
        UserRole.DEO => "Department Environmental Officer — quản lý cấp Tỉnh/TP",
        UserRole.LEO => "Local Environmental Officer — quản lý cấp Xã/Phường",
        UserRole.Cleanup => "Đội dọn dẹp — xử lý ô nhiễm rác/nước/hóa chất",
        UserRole.Inspector => "Đội thanh tra — xử phạt ô nhiễm tiếng ồn/không khí",
        UserRole.Admin => "Quản trị viên hệ thống — toàn quyền",
        _ => role.ToString()
    };
}

// ── Request DTOs ──
public sealed record AdminUpdateUserRequest(
    string? FullName, string? PhoneNumber, UserRole? Role, bool? IsEmailVerified);
public sealed record UpdateUserRoleRequest(UserRole NewRole);
public sealed record ForceUpdateStatusRequest(ReportStatus NewStatus, string Reason);
public sealed record UpdateCategoryRequest(string NameVi, string NameEn, string? IconUrl);
public sealed record ArchiveCategoryRequest(bool Archive);
public sealed record RoleDto(string Name, string Description);
public sealed record RolePermissionDto(string Role, List<string> Permissions);
