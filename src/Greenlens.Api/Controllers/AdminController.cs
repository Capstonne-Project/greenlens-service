using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Admin.ArchiveCategory;
using Greenlens.Application.Features.Admin.AuditLogs.ExportAuditLogs;
using Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogStats;
using Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogById;
using Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogs;
using Greenlens.Application.Features.Admin.CreateCategory;
using Greenlens.Application.Features.Admin.CreateWasteTag;
using Greenlens.Application.Features.Admin.DeleteCategory;
using Greenlens.Application.Features.Admin.DeleteWasteTag;
using Greenlens.Application.Features.Admin.ForceUpdateReportStatus;
using Greenlens.Application.Features.Admin.GetAdminReports;
using Greenlens.Application.Features.Admin.GetAdminWasteTags;
using Greenlens.Application.Features.Admin.GetAdminPollutionCategories;
using Greenlens.Application.Features.Admin.PenaltyFrameworks.CreatePenaltyFramework;
using Greenlens.Application.Features.Admin.PenaltyFrameworks.DeactivatePenaltyFramework;
using Greenlens.Application.Features.Admin.PenaltyFrameworks.GetPenaltyFrameworks;
using Greenlens.Application.Features.Admin.PenaltyFrameworks.UpdatePenaltyFramework;
using Greenlens.Application.Features.Admin.ToggleBanUser;
using Greenlens.Application.Features.Admin.ToggleWasteTag;
using Greenlens.Application.Features.Admin.UpdateCategory;
using Greenlens.Application.Features.Admin.UpdateWasteTag;
using Greenlens.Application.Features.Admin.UpdateUserRole;
using Greenlens.Application.Features.Admin.BlockedWords.CreateBlockedWord;
using Greenlens.Application.Features.Admin.BlockedWords.DeleteBlockedWord;
using Greenlens.Application.Features.Admin.BlockedWords.GetBlockedWords;
using Greenlens.Application.Features.Admin.BlockedWords.UpdateBlockedWord;
using Greenlens.Application.Features.Admin.ContentModeration.HideReport;
using Greenlens.Application.Features.Admin.ContentModeration.UnhideReport;
using Greenlens.Application.Features.Admin.SpamDashboard.GetSpamSuspects;
using Greenlens.Application.Features.Admin.GamificationConfigs.GetGamificationConfigs;
using Greenlens.Application.Features.Admin.GamificationConfigs.UpdateGamificationConfig;
using Greenlens.Application.Features.Admin.Badges.GetAdminBadges;
using Greenlens.Application.Features.Admin.Badges.UpdateBadge;
using Greenlens.Application.Features.Admin.Badges.UpdateBadgeThresholds;
using Greenlens.Application.Features.Admin.Badges.ToggleBadge;
using Greenlens.Application.Features.Admin.NotificationTemplates.CreateNotificationTemplate;
using Greenlens.Application.Features.Admin.NotificationTemplates.DeleteNotificationTemplate;
using Greenlens.Application.Features.Admin.NotificationTemplates.GetNotificationTemplateById;
using Greenlens.Application.Features.Admin.NotificationTemplates.GetNotificationTemplates;
using Greenlens.Application.Features.Admin.NotificationTemplates.PublishNotificationTemplate;
using Greenlens.Application.Features.Admin.NotificationTemplates.TestNotificationTemplate;
using Greenlens.Application.Features.Admin.NotificationTemplates.UpdateNotificationTemplate;
using Greenlens.Application.Features.Admin.SystemSettings.GetSystemSettingModules;
using Greenlens.Application.Features.Admin.SystemSettings.GetSystemSettings;
using Greenlens.Application.Features.Admin.SystemSettings.ResetSystemSettingsModule;
using Greenlens.Application.Features.Admin.SystemSettings.UpdateSystemSettings;
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
[Tags("⚙️ Admin Dashboard")]
public sealed class AdminController(ISender sender) : ControllerBase
{
    // ═══════════════════════════════════════════
    // ██  USERS
    // ═══════════════════════════════════════════

    [HttpPost("users")]
    [SwaggerOperation(Summary = "[Admin] Tạo tài khoản", Description = "Tạo tài khoản mới (Officer, Cleaner, Inspector, Citizen). Email tự động xác minh.")]
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<CreateAccountResponse>))]
    [SwaggerResponse(409, "Email đã tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAccountAsync(
        [FromBody] CreateAccountCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp("Đã tạo tài khoản thành công.");

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
    [SwaggerResponse(200, "Đã đổi role", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateUserRoleAsync(
        [FromRoute] Guid id, [FromBody] UpdateUserRoleRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateUserRoleCommand(id, request.NewRole), ct)).ToHttpNoContent("Đã đổi role thành công.");

    [HttpPut("users/{id:guid}/ban")]
    [SwaggerOperation(Summary = "[Admin] Cấm/bỏ cấm user", Description = "Toggle trạng thái ban tài khoản. Không thể tự cấm chính mình. Ghi audit log.")]
    [SwaggerResponse(200, "Đã cập nhật trạng thái ban", typeof(ApiResponse<ToggleBanUserResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    [SwaggerResponse(422, "Không thể cấm chính mình", typeof(ApiResponse))]
    public async Task<IActionResult> ToggleBanUserAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new ToggleBanUserCommand(id), ct)).ToHttp();

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
    [SwaggerResponse(200, "Đã cập nhật status", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> ForceUpdateStatusAsync(
        [FromRoute] Guid id, [FromBody] ForceUpdateStatusRequest request, CancellationToken ct)
        => (await sender.Send(
            new ForceUpdateReportStatusCommand(id, request.NewStatus, request.Reason), ct)).ToHttpNoContent("Đã cập nhật status báo cáo.");

    // ═══════════════════════════════════════════
    // ██  POLLUTION CATEGORIES
    // ═══════════════════════════════════════════

    [HttpGet("pollution-categories")]
    [SwaggerOperation(
        Summary = "[Admin] Danh sách danh mục ô nhiễm (phân trang)",
        Description = "Trả về tất cả pollution category (bao gồm inactive) cho Admin Dashboard. " +
            "Hỗ trợ tìm kiếm (code, tên VN, tên EN), lọc theo trạng thái active, " +
            "sắp xếp theo: code, nameVi, nameEn, isActive, reportCount, createdAt (mặc định: code). " +
            "Mỗi category kèm reportCount (số báo cáo đang sử dụng category này).")]
    [SwaggerResponse(200, "Danh sách category", typeof(ApiResponse<GetAdminPollutionCategoriesResponse>))]
    public async Task<IActionResult> GetAllPollutionCategoriesAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminPollutionCategoriesQuery(page, pageSize, search, isActive, sortBy, sortDesc), ct)).ToHttp();

    [HttpPost("pollution-categories")]
    [SwaggerOperation(Summary = "[Admin] Tạo danh mục ô nhiễm", Description = "Tạo loại ô nhiễm mới (code, tên VN, tên EN, icon URL).")]
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<CreateCategoryResponse>))]
    public async Task<IActionResult> CreateCategoryAsync(
        [FromBody] CreateCategoryCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp("Đã tạo danh mục ô nhiễm thành công.");

    [HttpPut("pollution-categories/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật danh mục ô nhiễm", Description = "Cập nhật tên VN, tên EN, icon URL của danh mục.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateCategoryAsync(
        [FromRoute] Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
        => (await sender.Send(
            new UpdateCategoryCommand(id, request.NameVi, request.NameEn, request.IconUrl), ct)).ToHttpNoContent("Đã cập nhật danh mục.");

    [HttpDelete("pollution-categories/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Xóa danh mục (soft-delete)", Description = "Xóa mềm danh mục. Các báo cáo cũ vẫn giữ reference. Danh mục sẽ không xuất hiện cho citizen nữa.")]
    [SwaggerResponse(200, "Đã xóa", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteCategoryAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new DeleteCategoryCommand(id), ct)).ToHttpNoContent("Đã xóa danh mục.");

    [HttpPut("pollution-categories/{id:guid}/archive")]
    [SwaggerOperation(Summary = "[Admin] Archive/Unarchive danh mục", Description = "Toggle trạng thái active/inactive. Body: { archive: true/false }.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> ArchiveCategoryAsync(
        [FromRoute] Guid id, [FromBody] ArchiveCategoryRequest request, CancellationToken ct)
        => (await sender.Send(new ArchiveCategoryCommand(id, request.Archive), ct)).ToHttpNoContent("Đã cập nhật trạng thái danh mục.");

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
            new("DEO", [
                "GET /dashboard/deo/*",
                "GET /departments/my/reports",
                "GET /departments/my-offices",
                "GET /reports/{id}",
                "GET /reports/export",
                "GET /reports/officer-kpi",
                "GET /reports/duplicate-candidates",
                "GET /companies",
                "POST /companies",
                "GET /companies/{id}/kpi"]),
            new("LEO", ["GET /offices", "GET /teams", "GET /reports", "PUT /reports/*/verify", "PUT /reports/*/reject", "POST /reports/*/assign", "PUT /reports/*/reassign", "GET /reports/queue"]),
            new("Cleaner", ["PUT /reports/*/resolve", "PUT /reports/*/decline"]),
            new("Inspector", ["PUT /reports/*/penalty", "PUT /reports/*/close-no-violation", "PUT /reports/*/decline"]),
            new("Citizen", ["POST /reports", "GET /reports/my", "PUT /reports/*/close", "PUT /reports/*/reopen"])
        };
        IActionResult result = Ok(new ApiResponse<List<RolePermissionDto>> { Data = matrix });
        return Task.FromResult(result);
    }

    // ═══════════════════════════════════════════
    // ██  WASTE TAGS MANAGEMENT
    // ═══════════════════════════════════════════

    [HttpGet("waste-tags")]
    [SwaggerOperation(
        Summary = "[Admin] Danh sách tag loại rác (phân trang)",
        Description = "Trả về tất cả waste tag (bao gồm inactive) cho Admin Dashboard. " +
            "Hỗ trợ tìm kiếm (code, tên VN, tên EN, mô tả), lọc theo trạng thái active, " +
            "sắp xếp theo: code, nameVi, nameEn, isActive, reportCount, createdAt (mặc định: displayOrder). " +
            "Mỗi tag kèm reportCount (số báo cáo đang sử dụng tag này).")]
    [SwaggerResponse(200, "Danh sách tag", typeof(ApiResponse<GetAdminWasteTagsResponse>))]
    public async Task<IActionResult> GetAllWasteTagsAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
        => (await sender.Send(new GetAdminWasteTagsQuery(page, pageSize, search, isActive, sortBy, sortDesc), ct)).ToHttp();

    [HttpPost("waste-tags")]
    [SwaggerOperation(Summary = "[Admin] Tạo tag loại rác mới", Description = "Tạo waste tag mới. Code phải viết HOA (UPPER_SNAKE_CASE), duy nhất.")]
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<CreateWasteTagResponse>))]
    [SwaggerResponse(409, "Code đã tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> CreateWasteTagAsync(
        [FromBody] CreateWasteTagCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp("Đã tạo tag loại rác thành công.");

    [HttpPut("waste-tags/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Sửa tag loại rác", Description = "Cập nhật tên, icon, mô tả, thứ tự hiển thị. Không thể đổi Code.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Tag không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateWasteTagAsync(
        [FromRoute] Guid id, [FromBody] AdminUpdateWasteTagRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateWasteTagCommand(
            id, request.NameVi, request.NameEn, request.IconUrl,
            request.Description, request.DisplayOrder), ct)).ToHttpNoContent("Đã cập nhật tag loại rác.");

    [HttpPatch("waste-tags/{id:guid}/toggle")]
    [SwaggerOperation(Summary = "[Admin] Bật/tắt tag loại rác", Description = "Vô hiệu hóa hoặc kích hoạt lại waste tag. Tag bị vô hiệu hóa sẽ không xuất hiện trong dropdown nhưng dữ liệu cũ vẫn giữ.")]
    [SwaggerResponse(200, "Đã thay đổi trạng thái", typeof(ApiResponse))]
    [SwaggerResponse(404, "Tag không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> ToggleWasteTagAsync(
        [FromRoute] Guid id, [FromBody] ToggleWasteTagRequest request, CancellationToken ct)
        => (await sender.Send(new ToggleWasteTagCommand(id, request.IsActive), ct)).ToHttpNoContent("Đã thay đổi trạng thái tag.");

    [HttpDelete("waste-tags/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Xóa tag loại rác (soft-delete)", Description = "Xóa mềm waste tag. Các báo cáo cũ vẫn giữ reference.")]
    [SwaggerResponse(200, "Đã xóa", typeof(ApiResponse))]
    [SwaggerResponse(404, "Tag không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteWasteTagAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new DeleteWasteTagCommand(id), ct)).ToHttpNoContent("Đã xóa tag loại rác.");

    // ═══════════════════════════════════════════
    // ██  PENALTY FRAMEWORKS (BR-ADM-008)
    // ═══════════════════════════════════════════

    [HttpGet("penalty-frameworks")]
    [SwaggerOperation(Summary = "[Admin] Danh sách khung tiền phạt", Description = "Danh sách khung mức phạt theo loại ô nhiễm và cấp vi phạm. Hỗ trợ lọc theo categoryId, violationLevel, isActive.")]
    [SwaggerResponse(200, "Danh sách khung phạt", typeof(ApiResponse<GetPenaltyFrameworksResponse>))]
    public async Task<IActionResult> GetPenaltyFrameworksAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Domain.Enums.ViolationLevel? violationLevel = null,
        [FromQuery] bool? isActive = null, CancellationToken ct = default)
        => (await sender.Send(
            new GetPenaltyFrameworksQuery(page, pageSize, categoryId, violationLevel, isActive), ct)).ToHttp();

    [HttpPost("penalty-frameworks")]
    [SwaggerOperation(Summary = "[Admin] Tạo khung tiền phạt", Description = "Tạo khung mức phạt mới cho 1 loại ô nhiễm + cấp vi phạm. MinAmount ≤ MaxAmount.")]
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<CreatePenaltyFrameworkResponse>))]
    [SwaggerResponse(409, "Đã tồn tại active entry cho category + level này", typeof(ApiResponse))]
    public async Task<IActionResult> CreatePenaltyFrameworkAsync(
        [FromBody] CreatePenaltyFrameworkCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp("Đã tạo khung tiền phạt thành công.");

    [HttpPut("penalty-frameworks/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật khung tiền phạt", Description = "Cập nhật mức min/max và ngày hiệu lực. Không ảnh hưởng quyết định đã ban hành.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdatePenaltyFrameworkAsync(
        [FromRoute] Guid id, [FromBody] UpdatePenaltyFrameworkRequest request, CancellationToken ct)
        => (await sender.Send(
            new UpdatePenaltyFrameworkCommand(id, request.MinAmount, request.MaxAmount, request.EffectiveFrom, request.EffectiveTo), ct))
            .ToHttpNoContent("Đã cập nhật khung tiền phạt.");

    [HttpPatch("penalty-frameworks/{id:guid}/toggle")]
    [SwaggerOperation(Summary = "[Admin] Bật/tắt khung tiền phạt", Description = "Deactivate hoặc reactivate khung phạt. Khung bị tắt không dùng cho quyết định mới.")]
    [SwaggerResponse(200, "Đã thay đổi trạng thái", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> TogglePenaltyFrameworkAsync(
        [FromRoute] Guid id, [FromBody] TogglePenaltyFrameworkRequest request, CancellationToken ct)
        => (await sender.Send(new DeactivatePenaltyFrameworkCommand(id, request.Activate), ct))
            .ToHttpNoContent("Đã thay đổi trạng thái khung phạt.");

    // ═══════════════════════════════════════════
    // ██  AUDIT LOGS (BR-ADM-010)
    // ═══════════════════════════════════════════

    [HttpGet("audit-logs")]
    [SwaggerOperation(Summary = "[Admin] Danh sách audit log", Description = "Danh sách hành động nhạy cảm được ghi log. Lọc theo userId, actorRole, entityType, action, ngày.")]
    [SwaggerResponse(200, "Danh sách audit log", typeof(ApiResponse<GetAuditLogsResponse>))]
    public async Task<IActionResult> GetAuditLogsAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? userId = null, [FromQuery] UserRole? actorRole = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
        => (await sender.Send(
            new GetAuditLogsQuery(page, pageSize, userId, actorRole, entityType, entityId, action, fromDate, toDate), ct)).ToHttp();

    [HttpGet("audit-logs/export")]
    [SwaggerOperation(Summary = "[Admin] Export audit log CSV", Description = "Export audit log trong khoảng ngày (bắt buộc fromDate/toDate, tối đa 90 ngày). Không export PII nhạy cảm.")]
    [SwaggerResponse(200, "File CSV")]
    public async Task<IActionResult> ExportAuditLogsAsync(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] Guid? userId = null,
        [FromQuery] UserRole? actorRole = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        CancellationToken ct = default)
    {
        var result = await sender.Send(
            new ExportAuditLogsQuery(fromDate, toDate, userId, actorRole, entityType, action), ct);

        if (!result.IsSuccess)
            return result.ToHttp();

        var data = result.Value!;
        return File(data.Content, data.ContentType, data.FileName);
    }

    [HttpGet("audit-logs/stats")]
    [SwaggerOperation(Summary = "[Admin] Thống kê audit log", Description = "Tổng số, top 10 action, phân bổ theo ngày trong khoảng (tối đa 90 ngày).")]
    [SwaggerResponse(200, "Thống kê audit log", typeof(ApiResponse<GetAuditLogStatsResponse>))]
    public async Task<IActionResult> GetAuditLogStatsAsync(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken ct = default)
        => (await sender.Send(new GetAuditLogStatsQuery(fromDate, toDate), ct)).ToHttp();

    [HttpGet("audit-logs/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Chi tiết audit log", Description = "Xem 1 bản ghi audit kèm OldValues/NewValues JSON.")]
    [SwaggerResponse(200, "Chi tiết audit log", typeof(ApiResponse<AuditLogDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetAuditLogByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetAuditLogByIdQuery(id), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  CONTENT MODERATION (BR-ADM-006)
    // ═══════════════════════════════════════════

    [HttpPost("reports/{id:guid}/hide")]
    [SwaggerOperation(Summary = "[Admin] Ẩn báo cáo", Description = "Admin ẩn báo cáo vi phạm khỏi công chúng. Reversible — dùng unhide để hiện lại.")]
    [SwaggerResponse(200, "Đã ẩn", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> HideReportAsync(
        [FromRoute] Guid id, [FromBody] HideReportRequest request, CancellationToken ct)
        => (await sender.Send(new HideReportCommand(id, request.Reason), ct))
            .ToHttpNoContent("Đã ẩn báo cáo.");

    [HttpPost("reports/{id:guid}/unhide")]
    [SwaggerOperation(Summary = "[Admin] Hiện lại báo cáo", Description = "Admin bỏ ẩn báo cáo — hiện lại cho công chúng.")]
    [SwaggerResponse(200, "Đã hiện lại", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UnhideReportAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new UnhideReportCommand(id), ct))
            .ToHttpNoContent("Đã hiện lại báo cáo.");

    // ═══════════════════════════════════════════
    // ██  SPAM DASHBOARD (BR-ADM-007)
    // ═══════════════════════════════════════════

    [HttpGet("spam-suspects")]
    [SwaggerOperation(Summary = "[Admin] Spam Dashboard", Description = "Danh sách tài khoản nghi spam theo heuristic rules (submit > submit_max_per_hour khi không truyền minReportsPerHour, reject >3/7d, AI flagged).")]
    [SwaggerResponse(200, "Danh sách suspect", typeof(ApiResponse<GetSpamSuspectsResponse>))]
    public async Task<IActionResult> GetSpamSuspectsAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? minReportsPerHour = null,
        [FromQuery] int minRejected7Days = 3,
        [FromQuery] int minAiFlagged = 2, CancellationToken ct = default)
        => (await sender.Send(
            new GetSpamSuspectsQuery(page, pageSize, minReportsPerHour, minRejected7Days, minAiFlagged), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  SYSTEM SETTINGS (BR-ADM-010)
    // ═══════════════════════════════════════════

    [HttpGet("system-settings/modules")]
    [SwaggerOperation(
        Summary = "[Admin] Danh mục module cấu hình hệ thống",
        Description = "Trả về sidebar modules cho Admin UI system configuration.")]
    [SwaggerResponse(200, "Danh sách module", typeof(ApiResponse<GetSystemSettingModulesResponse>))]
    public async Task<IActionResult> GetSystemSettingModulesAsync(CancellationToken ct)
        => (await sender.Send(new GetSystemSettingModulesQuery(), ct)).ToHttp();

    [HttpGet("system-settings")]
    [SwaggerOperation(
        Summary = "[Admin] Danh sách cấu hình hệ thống",
        Description = "Lọc theo module (route slug hoặc enum name, vd. reports, Reports).")]
    [SwaggerResponse(200, "Danh sách settings", typeof(ApiResponse<GetSystemSettingsResponse>))]
    public async Task<IActionResult> GetSystemSettingsAsync(
        [FromQuery] string? module = null, CancellationToken ct = default)
        => (await sender.Send(new GetSystemSettingsQuery(module), ct)).ToHttp();

    [HttpGet("system-settings/{module}")]
    [SwaggerOperation(
        Summary = "[Admin] Cấu hình theo module",
        Description = "Alias GET /system-settings?module={module}.")]
    [SwaggerResponse(200, "Settings trong module", typeof(ApiResponse<GetSystemSettingsResponse>))]
    public async Task<IActionResult> GetSystemSettingsByModuleAsync(
        [FromRoute] string module, CancellationToken ct)
        => (await sender.Send(new GetSystemSettingsQuery(module), ct)).ToHttp();

    [HttpPatch("system-settings/{module}")]
    [SwaggerOperation(
        Summary = "[Admin] Cập nhật cấu hình theo module",
        Description = "Bulk PATCH: body là object key→value (snake_case keys). Validate min/max và invalidate cache.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse<UpdateSystemSettingsResponse>))]
    [SwaggerResponse(400, "Giá trị không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateSystemSettingsAsync(
        [FromRoute] string module,
        [FromBody] Dictionary<string, string> values,
        CancellationToken ct)
        => (await sender.Send(new UpdateSystemSettingsCommand(module, values), ct))
            .ToHttp("Đã cập nhật cấu hình hệ thống.");

    [HttpPost("system-settings/{module}/reset")]
    [SwaggerOperation(
        Summary = "[Admin] Reset module về giá trị mặc định",
        Description = "Khôi phục tất cả keys trong module về DefaultValue đã seed.")]
    [SwaggerResponse(200, "Đã reset", typeof(ApiResponse<GetSystemSettingsResponse>))]
    public async Task<IActionResult> ResetSystemSettingsModuleAsync(
        [FromRoute] string module, CancellationToken ct)
        => (await sender.Send(new ResetSystemSettingsModuleCommand(module), ct))
            .ToHttp("Đã reset cấu hình module.");

    // ═══════════════════════════════════════════
    // ██  GAMIFICATION CONFIG (BR-ADM-005)
    // ═══════════════════════════════════════════

    [HttpGet("gamification-configs")]
    [SwaggerOperation(
        Summary = "[Admin] Cấu hình điểm gamification (phân trang)",
        Description = "Danh sách cấu hình điểm cho từng hành động. " +
            "Hỗ trợ tìm kiếm (actionType, mô tả), lọc theo isActive, " +
            "sắp xếp theo: actionType, points, isActive, createdAt, updatedAt (mặc định: actionType).")]
    [SwaggerResponse(200, "Danh sách config", typeof(ApiResponse<GetGamificationConfigsResponse>))]
    public async Task<IActionResult> GetGamificationConfigsAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
        => (await sender.Send(
            new GetGamificationConfigsQuery(page, pageSize, search, isActive, sortBy, sortDesc), ct)).ToHttp();

    [HttpPut("gamification-configs/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật điểm gamification", Description = "Thay đổi số điểm cho 1 hành động. Bật/tắt hành động.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateGamificationConfigAsync(
        [FromRoute] Guid id, [FromBody] UpdateGamificationConfigRequest request, CancellationToken ct)
        => (await sender.Send(
            new UpdateGamificationConfigCommand(id, request.Points, request.Description, request.IsActive), ct))
            .ToHttpNoContent("Đã cập nhật cấu hình điểm.");

    [HttpGet("badges")]
    [SwaggerOperation(
        Summary = "[Admin] Danh sách huy hiệu (badges, phân trang)",
        Description = "Trả về badge catalog (bao gồm inactive) cho Admin Dashboard. " +
            "Hỗ trợ tìm kiếm (code, tên VN, tên EN, mô tả), lọc theo isActive, " +
            "sắp xếp theo: code, nameVi, nameEn, isActive, requiredPoints, requiredReportCount, requiredStreakDays, requiredActionCount, createdAt (mặc định: code).")]
    [SwaggerResponse(200, "Danh sách badge", typeof(ApiResponse<GetAdminBadgesResponse>))]
    public async Task<IActionResult> GetAdminBadgesAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
        => (await sender.Send(
            new GetAdminBadgesQuery(page, pageSize, search, isActive, sortBy, sortDesc), ct)).ToHttp();

    [HttpPut("badges/{id:guid}")]
    [SwaggerOperation(
        Summary = "[Admin] Sửa nội dung huy hiệu",
        Description = "Cập nhật tên VN/EN, mô tả, icon URL. Ngưỡng điểm/báo cáo qua PATCH .../thresholds.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateBadgeAsync(
        [FromRoute] Guid id, [FromBody] AdminUpdateBadgeRequest request, CancellationToken ct)
        => (await sender.Send(
            new UpdateBadgeCommand(id, request.NameVi, request.NameEn, request.Description, request.IconUrl), ct))
            .ToHttpNoContent("Đã cập nhật huy hiệu.");

    [HttpPatch("badges/{id:guid}/thresholds")]
    [SwaggerOperation(
        Summary = "[Admin] Cập nhật ngưỡng huy hiệu",
        Description = "Đặt ngưỡng eligibility (điểm, số báo cáo, streak, hoặc action count tùy loại badge).")]
    [SwaggerResponse(200, "Đã cập nhật ngưỡng", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateBadgeThresholdsAsync(
        [FromRoute] Guid id,
        [FromBody] AdminUpdateBadgeThresholdsRequest request,
        CancellationToken ct)
        => (await sender.Send(new UpdateBadgeThresholdsCommand(id, request.Threshold), ct))
            .ToHttpNoContent("Đã cập nhật ngưỡng huy hiệu.");

    [HttpPatch("badges/{id:guid}/toggle")]
    [SwaggerOperation(
        Summary = "[Admin] Bật/tắt huy hiệu",
        Description = "Badge inactive sẽ không xuất hiện trong catalog công khai và không được auto-award mới.")]
    [SwaggerResponse(200, "Đã thay đổi trạng thái", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> ToggleBadgeAsync(
        [FromRoute] Guid id, [FromBody] ToggleBadgeRequest request, CancellationToken ct)
        => (await sender.Send(new ToggleBadgeCommand(id, request.IsActive), ct))
            .ToHttpNoContent("Đã thay đổi trạng thái huy hiệu.");

    // ═══════════════════════════════════════════
    // ██  NOTIFICATION TEMPLATES (BR-ADM-004)
    // ═══════════════════════════════════════════

    [HttpGet("notification-templates")]
    [SwaggerOperation(
        Summary = "[Admin] Danh sách template thông báo (phân trang)",
        Description = "Liệt kê notification templates. Lọc theo channel, isPublished, isActive. " +
            "Tìm kiếm theo templateKey, titleVi. " +
            "Sắp xếp theo: templateKey, titleVi, channel, type, isPublished, isActive, createdAt, updatedAt (mặc định: createdAt desc).")]
    [SwaggerResponse(200, "Danh sách template", typeof(ApiResponse<GetNotificationTemplatesResponse>))]
    public async Task<IActionResult> GetNotificationTemplatesAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] string? channel = null,
        [FromQuery] bool? isPublished = null, [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
        => (await sender.Send(
            new GetNotificationTemplatesQuery(
                page, pageSize, search, channel, isPublished, isActive, sortBy, sortDesc), ct)).ToHttp();

    [HttpPost("notification-templates")]
    [SwaggerOperation(Summary = "[Admin] Tạo template thông báo", Description = "Tạo template mới (draft). Cần publish trước khi hệ thống sử dụng.")]
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<CreateNotificationTemplateResponse>))]
    [SwaggerResponse(409, "Đã tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> CreateNotificationTemplateAsync(
        [FromBody] CreateNotificationTemplateCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp("Đã tạo template thông báo thành công.");

    [HttpPatch("notification-templates/{id:guid}/publish")]
    [SwaggerOperation(Summary = "[Admin] Publish/Unpublish template", Description = "Publish template để hệ thống dùng, hoặc unpublish để tạm tắt.")]
    [SwaggerResponse(200, "Đã thay đổi", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> PublishNotificationTemplateAsync(
        [FromRoute] Guid id, [FromBody] PublishTemplateRequest request, CancellationToken ct)
        => (await sender.Send(new PublishNotificationTemplateCommand(id, request.Publish), ct))
            .ToHttpNoContent(request.Publish ? "Đã publish template." : "Đã unpublish template.");

    [HttpPost("notification-templates/{id:guid}/test")]
    [SwaggerOperation(Summary = "[Admin] Test gửi template", Description = "Gửi thử template với dữ liệu mẫu đến admin hiện tại. Không gửi cho user thật.")]
    [SwaggerResponse(200, "Kết quả render + gửi thử", typeof(ApiResponse<TestNotificationTemplateResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> TestNotificationTemplateAsync(
        [FromRoute] Guid id, [FromBody] Dictionary<string, string> sampleData, CancellationToken ct)
        => (await sender.Send(new TestNotificationTemplateCommand(id, sampleData), ct)).ToHttp();

    [HttpGet("notification-templates/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Chi tiết template", Description = "Lấy chi tiết 1 template theo ID.")]
    [SwaggerResponse(200, "Chi tiết template", typeof(ApiResponse<NotificationTemplateDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetNotificationTemplateByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetNotificationTemplateByIdQuery(id), ct)).ToHttp();

    [HttpPut("notification-templates/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật template", Description = "Cập nhật nội dung template. Khi cập nhật xong hệ thống sẽ tự động set IsPublished = false.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateNotificationTemplateAsync(
        [FromRoute] Guid id, [FromBody] UpdateTemplateRequest request, CancellationToken ct)
        => (await sender.Send(
            new UpdateNotificationTemplateCommand(id, request.TitleVi, request.BodyVi, request.TitleEn, request.BodyEn), ct))
            .ToHttpNoContent("Đã cập nhật template.");

    [HttpDelete("notification-templates/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Xóa (vô hiệu hóa) template", Description = "Vô hiệu hóa (Deactivate) template không còn dùng nữa.")]
    [SwaggerResponse(200, "Đã xóa", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteNotificationTemplateAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new DeleteNotificationTemplateCommand(id), ct))
            .ToHttpNoContent("Đã xóa template.");

    // ═══════════════════════════════════════════
    // ██  BLOCKED WORDS (BR-REP-004, BR-CMT-003)
    // ═══════════════════════════════════════════

    [HttpGet("blocked-words")]
    [SwaggerOperation(
        Summary = "[Admin] Danh sách từ bị chặn",
        Description = "Quản lý bộ lọc tục tĩu cho mô tả báo cáo (BR-REP-004) và bình luận (BR-CMT-003).")]
    [SwaggerResponse(200, "Danh sách từ", typeof(ApiResponse<GetBlockedWordsResponse>))]
    public async Task<IActionResult> GetBlockedWordsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetBlockedWordsQuery(page, pageSize, search, isActive), ct)).ToHttp();

    [HttpPost("blocked-words")]
    [SwaggerOperation(Summary = "[Admin] Thêm từ bị chặn", Description = "Thêm từ/cụm từ vào bộ lọc. Áp dụng ngay sau khi lưu.")]
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<CreateBlockedWordResponse>))]
    [SwaggerResponse(409, "Từ đã tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> CreateBlockedWordAsync(
        [FromBody] CreateBlockedWordRequest request,
        CancellationToken ct)
        => (await sender.Send(new CreateBlockedWordCommand(request.Word, request.Note), ct)).ToHttp("Đã thêm từ bị chặn thành công.");

    [HttpPut("blocked-words/{id:guid}")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật từ bị chặn", Description = "Sửa nội dung từ, ghi chú, hoặc bật/tắt.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    [SwaggerResponse(409, "Trùng từ", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateBlockedWordAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateBlockedWordRequest request,
        CancellationToken ct)
        => (await sender.Send(
            new UpdateBlockedWordCommand(id, request.Word, request.Note, request.IsActive), ct))
            .ToHttpNoContent("Đã cập nhật từ bị chặn.");

    [HttpDelete("blocked-words/{id:guid}")]
    [SwaggerOperation(
        Summary = "[Admin] Vô hiệu hóa từ bị chặn",
        Description = "Đặt IsActive = false. Từ vẫn lưu trong DB để audit; không còn chặn nội dung.")]
    [SwaggerResponse(200, "Đã vô hiệu hóa", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteBlockedWordAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new DeleteBlockedWordCommand(id), ct))
            .ToHttpNoContent("Đã vô hiệu hóa từ bị chặn.");

    // ── Helpers ──
    private static string GetRoleDescription(UserRole role) => role switch
    {
        UserRole.Citizen => "Người dân — tạo và theo dõi báo cáo ô nhiễm",
        UserRole.DEO => "Điều phối viên cấp Tỉnh — giám sát báo cáo toàn tỉnh và quản lý công ty DVMT",
        UserRole.LEO => "Giám sát viên cấp Phường/Xã — nhận task, phân công và quản lý team",
        UserRole.Cleaner => "Đội dọn dẹp — xử lý ô nhiễm rác/nước/hóa chất",
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
public sealed record AdminUpdateWasteTagRequest(string NameVi, string NameEn, string? IconUrl, string? Description, int DisplayOrder);
public sealed record ToggleWasteTagRequest(bool IsActive);
public sealed record UpdatePenaltyFrameworkRequest(decimal MinAmount, decimal MaxAmount, DateTime EffectiveFrom, DateTime? EffectiveTo);
public sealed record TogglePenaltyFrameworkRequest(bool Activate);
public sealed record HideReportRequest(string Reason);
public sealed record UpdateGamificationConfigRequest(int Points, string Description, bool IsActive);
public sealed record AdminUpdateBadgeRequest(string NameVi, string NameEn, string? Description, string? IconUrl);
public sealed record AdminUpdateBadgeThresholdsRequest(int Threshold);
public sealed record ToggleBadgeRequest(bool IsActive);
public sealed record PublishTemplateRequest(bool Publish = true);
public sealed record UpdateTemplateRequest(string TitleVi, string BodyVi, string? TitleEn, string? BodyEn);
public sealed record CreateBlockedWordRequest(string Word, string? Note);
public sealed record UpdateBlockedWordRequest(string Word, string? Note, bool IsActive);
