using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.AnalyzeReportImage;
using Greenlens.Application.Features.Reports.AssignTeam;
using Greenlens.Application.Features.Reports.CloseNoViolation;
using Greenlens.Application.Features.Reports.CloseReport;
using Greenlens.Application.Features.Reports.DeclineAssignment;
using Greenlens.Application.Features.Reports.GetMyReports;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Application.Features.Reports.GetReportById;
using Greenlens.Application.Features.Reports.GetReportHistory;
using Greenlens.Application.Features.Reports.GetReports;
using Greenlens.Application.Features.Reports.IssuePenalty;
using Greenlens.Application.Features.Reports.ReassignTeam;
using Greenlens.Application.Features.Reports.RejectReport;
using Greenlens.Application.Features.Reports.ReopenReport;
using Greenlens.Application.Features.Reports.ResolveReport;
using Greenlens.Application.Features.Reports.SubmitPollutionReport;
using Greenlens.Application.Features.Reports.VerifyReport;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Quản lý Reports — CRUD, workflow lifecycle, queries.</summary>
[ApiController]
[Route("v1/reports")]
[Produces("application/json")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    // ═══════════════════════════════════════════
    // ██  AI ANALYZE (Step 1)
    // ═══════════════════════════════════════════

    [HttpPost("analyze")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "[Citizen/Anonymous] Phân tích ảnh trước khi tạo báo cáo (Step 1)",
        Description = "Upload ảnh để AI phân tích. Trả về temp_image_id (TTL 15 phút), kết quả AI, và " +
            "suggestedCategory (id, code, nameVi, nameEn) để FE auto-fill loại ô nhiễm. " +
            "Nếu decision = IRRELEVANT_OR_SUSPECTED_ABUSIVE → FE hiển thị warning, disable nút Submit. " +
            "AI Service down → 503. Ảnh chưa được lưu vĩnh viễn.")]
    [SwaggerResponse(200, "Kết quả phân tích", typeof(ApiResponse<AnalyzeReportImageResponse>))]
    [SwaggerResponse(400, "File rỗng hoặc sai định dạng")]
    [SwaggerResponse(413, "File > 20MB")]
    [SwaggerResponse(503, "AI Service tạm thời không khả dụng")]
    public async Task<IActionResult> AnalyzeAsync(IFormFile image, CancellationToken ct)
    {
        if (image is null || image.Length == 0)
            return BadRequest(new ApiResponse
            {
                Code = "FILE_REQUIRED",
                Message = "Vui lòng chọn file ảnh.",
                Status = 400
            });

        if (image.Length > 20 * 1024 * 1024)
            return StatusCode(413, new ApiResponse
            {
                Code = "FILE_TOO_LARGE",
                Message = "File ảnh vượt quá 20MB.",
                Status = 413
            });

        byte[] bytes;
        await using (var ms = new MemoryStream())
        {
            await image.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }

        var command = new AnalyzeReportImageCommand(bytes, image.FileName, image.ContentType, image.Length);
        return (await sender.Send(command, ct)).ToHttp();
    }

    // ═══════════════════════════════════════════
    // ██  CRUD
    // ═══════════════════════════════════════════

    [HttpPost]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "[Citizen/Anonymous] Tạo báo cáo ô nhiễm",
        Description = "Tạo báo cáo mới. Hỗ trợ anonymous (không cần login). " +
            "Hệ thống tự động gán SLA 24h và route báo cáo theo wardCode đến LocalOffice hoặc Department queue.")]
    [SwaggerResponse(201, "Đã tạo", typeof(ApiResponse<SubmitPollutionReportResponse>))]
    [SwaggerResponse(400, "Thiếu thông tin xác thực hoặc ward/province không hợp lệ", typeof(ApiResponse))]
    [SwaggerResponse(404, "Danh mục không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> SubmitAsync(
        [FromBody] SubmitPollutionReportCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpGet]
    [Authorize]
    [SwaggerOperation(Summary = "[Auth] Danh sách báo cáo", Description = "Trả về danh sách báo cáo ô nhiễm. Hỗ trợ lọc theo status, category, ward, severity.")]
    [SwaggerResponse(200, "Danh sách báo cáo", typeof(ApiResponse<GetReportsResponse>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] ReportStatus? status = null, [FromQuery] Guid? categoryId = null,
        [FromQuery] string? wardCode = null, [FromQuery] Severity? severity = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetReportsQuery(page, pageSize, status, categoryId, wardCode, severity), ct)).ToHttp();

    [HttpGet("{id:guid}")]
    [Authorize]
    [SwaggerOperation(Summary = "[Auth] Chi tiết báo cáo", Description = "Trả về full thông tin báo cáo kèm media, assignments, lịch sử status.")]
    [SwaggerResponse(200, "Chi tiết báo cáo", typeof(ApiResponse<ReportDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetReportByIdQuery(id), ct)).ToHttp();

    [HttpGet("my")]
    [Authorize]
    [SwaggerOperation(Summary = "[Citizen] Báo cáo của tôi", Description = "Trả về danh sách báo cáo do user hiện tại tạo. Hỗ trợ lọc theo status.")]
    [SwaggerResponse(200, "Danh sách báo cáo của tôi", typeof(ApiResponse<GetMyReportsResponse>))]
    public async Task<IActionResult> GetMyAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] ReportStatus? status = null, CancellationToken ct = default)
        => (await sender.Send(new GetMyReportsQuery(page, pageSize, status), ct)).ToHttp();

    [HttpGet("{id:guid}/history")]
    [Authorize]
    [SwaggerOperation(Summary = "[Auth] Lịch sử status báo cáo", Description = "Trả về timeline thay đổi status của báo cáo, kèm thông tin người thực hiện.")]
    [SwaggerResponse(200, "Lịch sử status", typeof(ApiResponse<GetReportHistoryResponse>))]
    public async Task<IActionResult> GetHistoryAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetReportHistoryQuery(id), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  OFFICER WORKFLOW
    // ═══════════════════════════════════════════

    [HttpPut("{id:guid}/verify")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [SwaggerOperation(Summary = "[LEO/DEO] Xác minh báo cáo", Description = "Officer kiểm tra thông tin và xác minh báo cáo. Có thể override severity và category nếu cần. Chuyển status Submitted → Verified.")]
    [SwaggerResponse(204, "Đã xác minh")]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    [SwaggerResponse(422, "Status không hợp lệ hoặc conflict of interest", typeof(ApiResponse))]
    public async Task<IActionResult> VerifyAsync(
        [FromRoute] Guid id, [FromBody] VerifyReportRequest request, CancellationToken ct)
        => (await sender.Send(new VerifyReportCommand(id, request.OverrideSeverity, request.OverrideCategoryId), ct))
            .ToHttpNoContent();

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [SwaggerOperation(Summary = "[LEO/DEO] Từ chối báo cáo", Description = "Officer từ chối báo cáo không hợp lệ. Yêu cầu lý do ≥ 20 ký tự. Chuyển status Submitted → Rejected.")]
    [SwaggerResponse(204, "Đã từ chối")]
    [SwaggerResponse(422, "Lý do quá ngắn hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> RejectAsync(
        [FromRoute] Guid id, [FromBody] RejectReportRequest request, CancellationToken ct)
        => (await sender.Send(new RejectReportCommand(id, request.Reason), ct)).ToHttpNoContent();

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [SwaggerOperation(Summary = "[LEO/DEO] Phân công team xử lý", Description = "Phân công 1 hoặc nhiều team cùng xử lý. Tất cả team ngang hàng. Team type phải khớp loại ô nhiễm. Chuyển status Verified → InProgress.")]
    [SwaggerResponse(204, "Đã phân công")]
    [SwaggerResponse(422, "Team type không khớp hoặc workload vượt quá", typeof(ApiResponse))]
    public async Task<IActionResult> AssignTeamAsync(
        [FromRoute] Guid id, [FromBody] AssignTeamRequest request, CancellationToken ct)
    {
        var items = request.Teams.Select(t => new TeamAssignmentItem(t.TeamId, t.Note)).ToList();
        return (await sender.Send(new AssignTeamCommand(id, items), ct)).ToHttpNoContent();
    }

    [HttpPut("{id:guid}/reassign")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [SwaggerOperation(Summary = "[LEO/DEO] Chuyển giao team", Description = "Chuyển assignment từ team cũ sang team mới cùng loại. Yêu cầu lý do ≥ 20 ký tự.")]
    [SwaggerResponse(204, "Đã chuyển giao")]
    [SwaggerResponse(422, "Khác loại team hoặc workload vượt quá", typeof(ApiResponse))]
    public async Task<IActionResult> ReassignAsync(
        [FromRoute] Guid id, [FromBody] ReassignTeamRequest request, CancellationToken ct)
        => (await sender.Send(new ReassignTeamCommand(id, request.OldTeamId, request.NewTeamId, request.Reason), ct))
            .ToHttpNoContent();

    [HttpGet("queue")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [SwaggerOperation(Summary = "[LEO/DEO] Xem hàng đợi báo cáo", Description = "Trả về danh sách báo cáo trong phạm vi quản lý, sắp theo điểm ưu tiên giảm dần.")]
    [SwaggerResponse(200, "Hàng đợi", typeof(ApiResponse<GetOfficerQueueResponse>))]
    public async Task<IActionResult> GetQueueAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] ReportStatus? status = null, CancellationToken ct = default)
        => (await sender.Send(new GetOfficerQueueQuery(page, pageSize, status), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  TEAM WORKFLOW
    // ═══════════════════════════════════════════

    [HttpPut("{id:guid}/resolve")]
    [Authorize(Roles = "Cleanup,Admin")]
    [SwaggerOperation(Summary = "[Cleanup] Hoàn thành phần việc của team", Description = "Cleanup Team đánh dấu phần việc đã hoàn thành. Yêu cầu ≥ 2 ảnh after. Khi tất cả team đều completed → report chuyển InProgress → Resolved.")]
    [SwaggerResponse(204, "Đã hoàn thành")]
    [SwaggerResponse(422, "Thiếu ảnh hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> ResolveAsync(
        [FromRoute] Guid id, [FromBody] ResolveReportRequest request, CancellationToken ct)
        => (await sender.Send(new ResolveReportCommand(id, request.TeamId, request.AfterImageUrls), ct))
            .ToHttpNoContent();

    [HttpPut("{id:guid}/penalty")]
    [Authorize(Roles = "Inspector,Admin")]
    [SwaggerOperation(Summary = "[Inspector] Xử phạt vi phạm", Description = "Inspection Team Leader ban hành quyết định xử phạt. Khi tất cả team đều completed → report chuyển InProgress → PenaltyIssued.")]
    [SwaggerResponse(204, "Đã xử phạt")]
    [SwaggerResponse(422, "Status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> IssuePenaltyAsync(
        [FromRoute] Guid id, [FromBody] IssuePenaltyRequest request, CancellationToken ct)
        => (await sender.Send(new IssuePenaltyCommand(id, request.TeamId), ct)).ToHttpNoContent();

    [HttpPut("{id:guid}/close-no-violation")]
    [Authorize(Roles = "Inspector,Admin")]
    [SwaggerOperation(Summary = "[Inspector] Đóng — không vi phạm", Description = "Inspection Team đóng báo cáo khi khảo sát không phát hiện vi phạm. Yêu cầu lý do ≥ 50 ký tự.")]
    [SwaggerResponse(204, "Đã đóng")]
    [SwaggerResponse(422, "Lý do quá ngắn hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> CloseNoViolationAsync(
        [FromRoute] Guid id, [FromBody] CloseNoViolationRequest request, CancellationToken ct)
        => (await sender.Send(new CloseNoViolationCommand(id, request.Reason), ct)).ToHttpNoContent();

    [HttpPut("{id:guid}/decline")]
    [Authorize(Roles = "Cleanup,Inspector,Admin")]
    [SwaggerOperation(Summary = "[Cleanup/Inspector] Từ chối task", Description = "Team từ chối task trong vòng 2 giờ sau khi được phân công. Yêu cầu lý do ≥ 20 ký tự.")]
    [SwaggerResponse(204, "Đã từ chối")]
    [SwaggerResponse(422, "Quá 2h hoặc lý do quá ngắn", typeof(ApiResponse))]
    public async Task<IActionResult> DeclineAsync(
        [FromRoute] Guid id, [FromBody] DeclineAssignmentRequest request, CancellationToken ct)
        => (await sender.Send(new DeclineAssignmentCommand(id, request.TeamId, request.Reason), ct))
            .ToHttpNoContent();

    // ═══════════════════════════════════════════
    // ██  CITIZEN WORKFLOW
    // ═══════════════════════════════════════════

    [HttpPut("{id:guid}/close")]
    [Authorize]
    [SwaggerOperation(Summary = "[Citizen/Auto] Đóng báo cáo", Description = "Citizen xác nhận hài lòng hoặc hệ thống tự động đóng sau 7 ngày. Chuyển status Resolved/PenaltyIssued → Closed.")]
    [SwaggerResponse(204, "Đã đóng")]
    [SwaggerResponse(422, "Status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> CloseAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new CloseReportCommand(id), ct)).ToHttpNoContent();

    [HttpPut("{id:guid}/reopen")]
    [Authorize]
    [SwaggerOperation(Summary = "[Citizen] Mở lại báo cáo", Description = "Citizen mở lại báo cáo nếu chưa hài lòng. Tối đa 2 lần reopen. Chuyển status Resolved → InProgress.")]
    [SwaggerResponse(204, "Đã mở lại")]
    [SwaggerResponse(422, "Hết lượt reopen hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> ReopenAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new ReopenReportCommand(id), ct)).ToHttpNoContent();
}

// ── Request DTOs ──
public sealed record VerifyReportRequest(Severity? OverrideSeverity = null, Guid? OverrideCategoryId = null);
public sealed record RejectReportRequest(string Reason);
public sealed record AssignTeamRequest(List<AssignTeamItemRequest> Teams);
public sealed record AssignTeamItemRequest(Guid TeamId, string? Note);
public sealed record ReassignTeamRequest(Guid OldTeamId, Guid NewTeamId, string Reason);
public sealed record ResolveReportRequest(Guid TeamId, List<string> AfterImageUrls);
public sealed record IssuePenaltyRequest(Guid TeamId);
public sealed record CloseNoViolationRequest(string Reason);
public sealed record DeclineAssignmentRequest(Guid TeamId, string Reason);
