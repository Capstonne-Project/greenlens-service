using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.AnalyzeReportImage;
using Greenlens.Application.Features.Reports.AnalyzeUploadedReportImage;
using Greenlens.Application.Features.Reports.AssignCompanyTeam;
using Greenlens.Application.Features.Reports.AssignTeam;
using Greenlens.Application.Features.Reports.DispatchToCompany;
using Greenlens.Application.Features.Reports.EscalateReport;
using Greenlens.Application.Features.Reports.GetCompanyQueue;
using Greenlens.Application.Features.Reports.GetCompanyAssignments;
using Greenlens.Application.Features.Reports.GetCompanyReportDetail;
using Greenlens.Application.Features.Reports.CloseReport;
using Greenlens.Application.Features.Reports.ConfirmDuplicate;
using Greenlens.Application.Features.Reports.DeleteDraft;
using Greenlens.Application.Features.Reports.DeleteReport;
using Greenlens.Application.Features.Reports.DismissDuplicate;
using Greenlens.Application.Features.Reports.ExportReports;
using Greenlens.Application.Features.Reports.FlagReport;
using Greenlens.Application.Features.Reports.GetDuplicateCandidates;
using Greenlens.Application.Features.Reports.GetMyDrafts;
using Greenlens.Application.Features.Reports.GetMyReports;
using Greenlens.Application.Features.Reports.GetOfficerKpi;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Application.Features.Reports.GetReportProgress;
using Greenlens.Application.Features.Reports.GetReportProgressBoard;
using Greenlens.Application.Features.Reports.GetReportById;
using Greenlens.Application.Features.Reports.GetReportHistory;
using Greenlens.Application.Features.Reports.GetReports;
using Greenlens.Application.Features.Reports.GetWasteTags;
using Greenlens.Application.Features.Reports.ReassignTeam;
using Greenlens.Application.Features.Reports.RejectReport;
using Greenlens.Application.Features.Reports.RateReport;
using Greenlens.Application.Features.Reports.ApproveReopenRequest;
using Greenlens.Application.Features.Reports.RejectReopenRequest;
using Greenlens.Application.Features.Reports.GetReopenRequests;
using Greenlens.Application.Features.Reports.RequestReopenReport;
using Greenlens.Application.Features.Reports.ReopenReport;
using Greenlens.Domain.Enums;
using Greenlens.Application.Features.Reports.ResolveReport;
using Greenlens.Application.Features.Reports.SaveDraft;
using Greenlens.Application.Features.Reports.SubmitPollutionReport;
using Greenlens.Application.Features.Reports.TagReportWaste;
using Greenlens.Application.Features.Reports.UpdateProgress;
using Greenlens.Application.Features.Reports.UploadBeforeImages;
using Greenlens.Application.Features.Reports.VerifyReport;
using Greenlens.Application.Features.Inspection.CreateInspectionReport;
using Greenlens.Application.Features.Inspection.GetInspectionsByReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Quản lý Reports — CRUD, workflow lifecycle, queries.</summary>
[ApiController]
[Route("v1/reports")]
[Produces("application/json")]
public sealed class ReportsController(
    ISender sender,
    ILogger<ReportsController> logger) : ControllerBase
{
    // ═══════════════════════════════════════════
    // ██  AI ANALYZE (Step 1)
    // ═══════════════════════════════════════════

    [HttpPost("analyze")]
    [Authorize(Roles = "Citizen")]
    [Tags("📋 Reports — Citizen Flow")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "[Citizen] Phân tích ảnh trước khi tạo báo cáo (Step 1)",
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

    [HttpPost("analyze-uploaded")]
    [Authorize(Roles = "Citizen")]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Citizen] Phân tích ảnh đã upload trực tiếp R2",
        Description =
            "Flow tối ưu: POST /v1/media/presign (ReportImage) → PUT file thẳng R2 → gọi endpoint này. " +
            "BE đọc object riêng tư từ R2 và gửi AI; không upload ảnh qua GreenLens API lần hai. " +
            "Trả tempImageId TTL 15 phút để submit kèm images[].")]
    [SwaggerResponse(200, "Kết quả phân tích", typeof(ApiResponse<AnalyzeReportImageResponse>))]
    [SwaggerResponse(400, "URL/metadata/type không hợp lệ", typeof(ApiResponse))]
    [SwaggerResponse(404, "Object R2 không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(500, "AI Service tạm thời không khả dụng", typeof(ApiResponse))]
    public async Task<IActionResult> AnalyzeUploadedAsync(
        [FromBody] AnalyzeUploadedReportImageCommand command,
        CancellationToken ct)
    {
        logger.LogInformation(
            "[AI-DIAG] HTTP POST /v1/reports/analyze-uploaded received | file={FileName} key={Key} sizeBytes={SizeBytes}",
            command.FileName,
            command.Key,
            command.SizeBytes);
        return (await sender.Send(command, ct)).ToHttp();
    }

    // ═══════════════════════════════════════════
    // ██  CRUD
    // ═══════════════════════════════════════════

    [HttpPost]
    [Authorize(Roles = "Citizen")]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Citizen] Tạo báo cáo ô nhiễm",
        Description = "Tạo báo cáo mới. " +
            "Hệ thống tự động gán SLA 24h và route báo cáo theo wardCode đến LocalOffice hoặc Department queue. " +
            "Có thể đính kèm wasteTagIds (optional) để citizen tự gắn loại rác — DEO có thể bổ sung/sửa sau.")]
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<SubmitPollutionReportResponse>))]
    [SwaggerResponse(400, "Thiếu thông tin xác thực hoặc ward/province không hợp lệ", typeof(ApiResponse))]
    [SwaggerResponse(404, "Danh mục không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> SubmitAsync(
        [FromBody] SubmitPollutionReportCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp("Đã gửi báo cáo thành công.");

    [HttpGet]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
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
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(Summary = "[Auth] Chi tiết báo cáo", Description = "Trả về full thông tin báo cáo kèm media, assignments, lịch sử status.")]
    [SwaggerResponse(200, "Chi tiết báo cáo", typeof(ApiResponse<ReportDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetReportByIdQuery(id), ct)).ToHttp();

    [HttpGet("my")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(Summary = "[Citizen] Báo cáo của tôi", Description = "Trả về danh sách báo cáo do user hiện tại tạo. Hỗ trợ lọc theo status.")]
    [SwaggerResponse(200, "Danh sách báo cáo của tôi", typeof(ApiResponse<GetMyReportsResponse>))]
    public async Task<IActionResult> GetMyAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] ReportStatus? status = null, CancellationToken ct = default)
        => (await sender.Send(new GetMyReportsQuery(page, pageSize, status), ct)).ToHttp();

    [HttpGet("{id:guid}/history")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(Summary = "[Auth] Lịch sử status báo cáo", Description = "Trả về timeline thay đổi status của báo cáo, kèm thông tin người thực hiện.")]
    [SwaggerResponse(200, "Lịch sử status", typeof(ApiResponse<GetReportHistoryResponse>))]
    public async Task<IActionResult> GetHistoryAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetReportHistoryQuery(id), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  OFFICER WORKFLOW
    // ═══════════════════════════════════════════

    [HttpPut("{id:guid}/verify")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(Summary = "[LEO] Xác minh báo cáo", Description = "LEO kiểm tra thông tin và xác minh báo cáo. Có thể override severity và category nếu cần. Chuyển status Submitted → Verified.")]
    [SwaggerResponse(200, "Đã xác minh", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    [SwaggerResponse(422, "Status không hợp lệ hoặc conflict of interest", typeof(ApiResponse))]
    public async Task<IActionResult> VerifyAsync(
        [FromRoute] Guid id, [FromBody] VerifyReportRequest request, CancellationToken ct)
        => (await sender.Send(new VerifyReportCommand(id, request.OverrideSeverity, request.OverrideCategoryId, request.WasteTagIds), ct))
            .ToHttpNoContent("Đã xác minh báo cáo thành công.");

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(Summary = "[LEO] Từ chối báo cáo", Description = "LEO từ chối báo cáo không hợp lệ. Yêu cầu lý do ≥ 20 ký tự. Chuyển status Submitted → Rejected.")]
    [SwaggerResponse(200, "Đã từ chối", typeof(ApiResponse))]
    [SwaggerResponse(422, "Lý do quá ngắn hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> RejectAsync(
        [FromRoute] Guid id, [FromBody] RejectReportRequest request, CancellationToken ct)
        => (await sender.Send(new RejectReportCommand(id, request.Reason), ct)).ToHttpNoContent("Đã từ chối báo cáo.");

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(Summary = "[LEO] Phân công team xử lý", Description = "LEO phân công 1 hoặc nhiều team cùng xử lý. Dispatch theo nhu cầu (không ràng buộc team type). Chuyển status Verified → InProgress.")]
    [SwaggerResponse(200, "Đã phân công", typeof(ApiResponse))]
    [SwaggerResponse(422, "Team type không khớp hoặc workload vượt quá", typeof(ApiResponse))]
    public async Task<IActionResult> AssignTeamAsync(
        [FromRoute] Guid id, [FromBody] AssignTeamRequest request, CancellationToken ct)
    {
        var items = request.Teams.Select(t => new TeamAssignmentItem(t.TeamId, t.Note)).ToList();
        return (await sender.Send(new AssignTeamCommand(id, items, request.WasteTagIds), ct)).ToHttpNoContent("Đã phân công team thành công.");
    }

    [HttpPut("{id:guid}/reassign")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(Summary = "[LEO] Chuyển giao team", Description = "Chuyển assignment từ team cũ sang team mới cùng loại. Yêu cầu lý do ≥ 20 ký tự.")]
    [SwaggerResponse(200, "Đã chuyển giao", typeof(ApiResponse))]
    [SwaggerResponse(422, "Khác loại team hoặc workload vượt quá", typeof(ApiResponse))]
    public async Task<IActionResult> ReassignAsync(
        [FromRoute] Guid id, [FromBody] ReassignTeamRequest request, CancellationToken ct)
        => (await sender.Send(new ReassignTeamCommand(id, request.OldTeamId, request.NewTeamId, request.Reason), ct))
            .ToHttpNoContent("Đã chuyển giao team thành công.");

    [HttpPost("{id:guid}/escalate")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Escalate báo cáo lên DEO (tuyến cấp TP)",
        Description = "BR-ORG-016: LEO xác minh xong, nhận thấy báo cáo thuộc tuyến đường cấp TP " +
            "(CITENCO territory) → escalate lên hàng đợi DEO. " +
            "Report phải ở trạng thái Verified hoặc InProgress. " +
            "Yêu cầu lý do ≥ 10 ký tự.")]
    [SwaggerResponse(200, "Đã escalate lên DEO", typeof(ApiResponse))]
    [SwaggerResponse(404, "Báo cáo không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Trạng thái không hợp lệ hoặc ngoài jurisdiction", typeof(ApiResponse))]
    public async Task<IActionResult> EscalateAsync(
        [FromRoute] Guid id, [FromBody] EscalateReportRequest request, CancellationToken ct)
        => (await sender.Send(new EscalateReportCommand(id, request.Reason), ct))
            .ToHttpNoContent("Đã chuyển báo cáo lên hàng đợi DEO.");

    // ═══════════════════════════════════════════
    // ██  COMPANY DISPATCH (v1.3)
    // ═══════════════════════════════════════════

    [HttpPost("{id:guid}/dispatch-to-company")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Điều phối task đến công ty",
        Description = "LEO điều phối báo cáo đã xác minh đến công ty dịch vụ môi trường. Chuyển Verified → InProgress; CompanyManager phân công team sau.")]
    [SwaggerResponse(204, "Đã điều phối")]
    [SwaggerResponse(422, "Công ty không hoạt động hoặc hết hợp đồng", typeof(ApiResponse))]
    public async Task<IActionResult> DispatchToCompanyAsync(
        [FromRoute] Guid id, [FromBody] DispatchToCompanyRequest request, CancellationToken ct)
        => (await sender.Send(new DispatchToCompanyCommand(id, request.CompanyId, request.Note), ct))
            .ToHttpNoContent("Đã điều phối task đến công ty thành công.");

    [HttpPost("{id:guid}/assign-company-team")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Phân công team công ty",
        Description = "CompanyManager phân công team của công ty mình cho báo cáo đã được LEO điều phối (status InProgress).")]
    [SwaggerResponse(204, "Đã phân công team")]
    [SwaggerResponse(422, "Team không thuộc công ty hoặc workload vượt quá", typeof(ApiResponse))]
    public async Task<IActionResult> AssignCompanyTeamAsync(
        [FromRoute] Guid id, [FromBody] AssignCompanyTeamRequest request, CancellationToken ct)
    {
        var items = request.Teams.Select(t => new TeamAssignmentItem(t.TeamId, t.Note)).ToList();
        return (await sender.Send(new AssignCompanyTeamCommand(id, items), ct))
            .ToHttpNoContent("Đã phân công team công ty thành công.");
    }

    [HttpGet("company-queue")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Danh sách task chờ phân công",
        Description = "CompanyManager xem các báo cáo đã được LEO điều phối, chờ phân công team (InProgress + AssignedCompanyId, chưa có assignment).")]
    [SwaggerResponse(200, "Danh sách task", typeof(ApiResponse<GetCompanyQueueResponse>))]
    public async Task<IActionResult> GetCompanyQueueAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Severity? severity = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyQueueQuery(page, pageSize, severity), ct)).ToHttp();

    [HttpGet("company-assignments")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Theo dõi task đã phân công",
        Description = "Xem toàn bộ task đã phân công cho team của công ty: team nào → báo cáo nào, " +
            "tiến độ (%), trạng thái assignment (Assigned/InProgress/Completed/Declined), " +
            "SLA deadline, và thông tin người phân công. " +
            "Lọc theo: assignmentStatus, reportStatus, search (mã báo cáo, địa chỉ, tên team).")]
    [SwaggerResponse(200, "Danh sách assignment", typeof(ApiResponse<GetCompanyAssignmentsResponse>))]
    public async Task<IActionResult> GetCompanyAssignmentsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AssignmentStatus? status = null,
        [FromQuery] ReportStatus? reportStatus = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyAssignmentsQuery(page, pageSize, status, reportStatus, search), ct)).ToHttp();

    [HttpGet("company-assignments/{reportId:guid}")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Chi tiết tiến độ xử lý báo cáo",
        Description = "Xem chi tiết 1 báo cáo: thông tin báo cáo, danh sách team được phân công (kèm thành viên, team leader), " +
            "tiến độ từng team (%), timeline chuyển trạng thái, ảnh/video, và waste tags.")]
    [SwaggerResponse(200, "Chi tiết tiến độ báo cáo", typeof(ApiResponse<CompanyReportDetailResponse>))]
    [SwaggerResponse(404, "Báo cáo không tồn tại hoặc không thuộc công ty của bạn", typeof(ApiResponse))]
    public async Task<IActionResult> GetCompanyReportDetailAsync(
        [FromRoute] Guid reportId, CancellationToken ct)
        => (await sender.Send(new GetCompanyReportDetailQuery(reportId), ct)).ToHttp();

    [HttpGet("progress-board")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Board tổng quan các báo cáo đang xử lý",
        Description = "Trả về danh sách card báo cáo InProgress trong office của LEO, " +
            "mỗi card gồm: SLA countdown, tiến độ tổng thể, avatar top-3 team leader. " +
            "Dùng cho trang dashboard dạng grid. " +
            "hoursRemaining âm = đã breach SLA. " +
            "Lọc thêm: severity, slaBreachedOnly=true.")]
    [SwaggerResponse(200, "Danh sách card báo cáo", typeof(ApiResponse<GetReportProgressBoardResponse>))]
    public async Task<IActionResult> GetProgressBoardAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Severity? severity = null,
        [FromQuery] bool slaBreachedOnly = false,
        CancellationToken ct = default)
        => (await sender.Send(
            new GetReportProgressBoardQuery(page, pageSize, severity, slaBreachedOnly), ct)).ToHttp();

    [HttpGet("{id:guid}/progress")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Tiến trình xử lý báo cáo",
        Description = "Trả về toàn bộ thông tin tiến trình của một báo cáo đang InProgress: " +
            "trạng thái từng team, người gán team (assignedById/assignedByName), % hoàn thành, ảnh tiến trình/nghiệm thu, SLA còn lại, và lịch sử status. " +
            "overallProgressPercent là trung bình của các assignment đang active (Completed = 100%). " +
            "sla.hoursRemaining âm = đã breach. " +
            "media.progressImages gom tất cả ảnh tiến trình từ mọi team.")]
    [SwaggerResponse(200, "Tiến trình báo cáo", typeof(ApiResponse<ReportProgressResponse>))]
    [SwaggerResponse(404, "Không tìm thấy báo cáo", typeof(ApiResponse))]
    public async Task<IActionResult> GetProgressAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetReportProgressQuery(id), ct)).ToHttp();

    [HttpGet("queue")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [Tags("🔍 DEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO/DEO] Xem hàng đợi báo cáo",
        Description = "Trả về danh sách báo cáo trong phạm vi quản lý. " +
            "Hỗ trợ search (code, address, category), filter (status, severity, category, ward, date range, SLA breached, isPossibleDuplicate), " +
            "và sort (priorityScore, createdAt, severity, slaVerifyDueAt, slaResolveDueAt — asc/desc). " +
            "Default sort: priorityScore desc.")]
    [SwaggerResponse(200, "Hàng đợi", typeof(ApiResponse<GetOfficerQueueResponse>))]
    public async Task<IActionResult> GetQueueAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        // Filters
        [FromQuery] ReportStatus? status = null,
        [FromQuery] Severity? severity = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? wardCode = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] bool? slaBreached = null,
        [FromQuery] bool? isPossibleDuplicate = null,
        [FromQuery] bool? hasPendingReopenRequest = null,
        // Search
        [FromQuery] string? search = null,
        // Sort
        [FromQuery] QueueSortBy sortBy = QueueSortBy.PriorityScore,
        [FromQuery] SortDirection sortDir = SortDirection.Desc,
        CancellationToken ct = default)
        => (await sender.Send(new GetOfficerQueueQuery(
            page, pageSize, status, severity, categoryId, wardCode,
            fromDate, toDate, slaBreached, isPossibleDuplicate, hasPendingReopenRequest, search, sortBy, sortDir), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  TEAM WORKFLOW
    // ═══════════════════════════════════════════
    // Xem: GET/PUT /v1/teams/my-tasks/* và GET /v1/teams/my-progress

    [HttpPut("{id:guid}/progress")]
    [Authorize(Roles = "Cleaner,CompanyStaff,Inspector,Admin")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff/Inspector] Cập nhật tiến độ + URL ảnh (presign)",
        Description = "Team leader cập nhật % tiến độ, ghi chú, và tùy chọn danh sách publicUrl ảnh đã PUT lên R2 " +
            "(qua POST /v1/media/presign purpose=Progress). Tối đa 5 URL. Status không thay đổi.")]
    [SwaggerResponse(200, "Đã cập nhật, trả về URLs ảnh đã lưu", typeof(ApiResponse<UpdateProgressResponse>))]
    [SwaggerResponse(422, "Không phải leader, assignment không InProgress, hoặc percent ngoài khoảng 0–100", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateProgressAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateProgressRequest request,
        CancellationToken ct)
        => (await sender.Send(new UpdateProgressCommand(
            id,
            request.ProgressPercent,
            request.ProgressNote,
            request.ImageUrls ?? []), ct)).ToHttp();

    [HttpPut("{id:guid}/resolve")]
    [Authorize(Roles = "Cleaner,CompanyStaff,Admin")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(Summary = "[Cleaner/CompanyStaff] Hoàn thành phần việc của team", Description = "Cleanup / company team leader đánh dấu phần việc đã hoàn thành. Yêu cầu ≥ 2 ảnh after. Khi tất cả team đều completed → report chuyển InProgress → Resolved.")]
    [SwaggerResponse(200, "Đã hoàn thành", typeof(ApiResponse))]
    [SwaggerResponse(422, "Thiếu ảnh hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> ResolveAsync(
        [FromRoute] Guid id, [FromBody] ResolveReportRequest request, CancellationToken ct)
        => (await sender.Send(new ResolveReportCommand(id, request.AfterImageUrls), ct))
            .ToHttpNoContent();

    // ── IssuePenalty & CloseNoViolation — MOVED to InspectionReport controller (v3.0) ──

    // ═══════════════════════════════════════════
    // ██  WASTE TAGS
    // ═══════════════════════════════════════════

    [HttpPut("{id:guid}/waste-tags")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Gắn tag loại rác cho báo cáo",
        Description = "LEO gắn tag loại rác (household, medical, hazardous,...) để cleanup team biết cần chuẩn bị gì. " +
            "Thay thế toàn bộ tag cũ bằng danh sách mới. Tối thiểu 1, tối đa 12 tag.")]
    [SwaggerResponse(200, "Đã gắn tag", typeof(ApiResponse))]
    [SwaggerResponse(404, "Report hoặc tag không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(422, "Status không hợp lệ hoặc tag bị vô hiệu hóa", typeof(ApiResponse))]
    public async Task<IActionResult> TagWasteAsync(
        [FromRoute] Guid id, [FromBody] TagWasteRequest request, CancellationToken ct)
        => (await sender.Send(new TagReportWasteCommand(id, request.WasteTagIds), ct)).ToHttpNoContent("Đã gắn tag loại rác thành công.");

    [HttpGet("~/v1/waste-tags")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Auth] Danh sách loại rác",
        Description = "Trả về tất cả waste tag đang active, sắp theo displayOrder. Dùng cho dropdown/chip selection trên FE.")]
    [SwaggerResponse(200, "Danh sách tag", typeof(ApiResponse<GetWasteTagsResponse>))]
    public async Task<IActionResult> GetWasteTagsAsync(CancellationToken ct)
        => (await sender.Send(new GetWasteTagsQuery(), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  DUPLICATE DETECTION (BR-REP-030..033)
    // ═══════════════════════════════════════════

    [HttpGet("duplicate-candidates")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO/DEO] Danh sách báo cáo nghi ngờ trùng lặp",
        Description = "BR-REP-031: Trả về các báo cáo bị gắn cờ possible_duplicate (Tier 1 geo/time hoặc Tier 2 AI) " +
            "kèm thông tin báo cáo gốc để LEO so sánh và quyết định gộp/bác bỏ.")]
    [SwaggerResponse(200, "Danh sách nghi ngờ trùng lặp", typeof(ApiResponse<GetDuplicateCandidatesResponse>))]
    public async Task<IActionResult> GetDuplicateCandidatesAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => (await sender.Send(new GetDuplicateCandidatesQuery(page, pageSize), ct)).ToHttp();

    [HttpPost("{id:guid}/confirm-duplicate")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO/DEO] Xác nhận & gộp báo cáo trùng lặp",
        Description = "BR-REP-032: LEO xác nhận báo cáo là trùng lặp của một báo cáo gốc. " +
            "Báo cáo gốc phải đã Verified (hoặc InProgress); báo cáo trùng có thể đang Submitted. " +
            "Báo cáo trùng chuyển sang Duplicate, tăng reporter count của báo cáo gốc, và cộng điểm cho người gửi.")]
    [SwaggerResponse(200, "Đã gộp báo cáo trùng", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy báo cáo hoặc báo cáo gốc", typeof(ApiResponse))]
    [SwaggerResponse(422, "Trạng thái không hợp lệ hoặc gộp vào chính nó", typeof(ApiResponse))]
    public async Task<IActionResult> ConfirmDuplicateAsync(
        [FromRoute] Guid id, [FromBody] ConfirmDuplicateRequest request, CancellationToken ct)
        => (await sender.Send(new ConfirmDuplicateCommand(id, request.PrimaryReportId), ct))
            .ToHttpNoContent("Đã gộp báo cáo trùng lặp.");

    [HttpPost("{id:guid}/dismiss-duplicate")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO/DEO] Bác bỏ cờ nghi ngờ trùng lặp",
        Description = "BR-REP-031: LEO xem xét và xác định báo cáo KHÔNG trùng lặp → xóa cờ possible_duplicate.")]
    [SwaggerResponse(200, "Đã bác bỏ", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy báo cáo", typeof(ApiResponse))]
    [SwaggerResponse(422, "Báo cáo không ở trạng thái nghi ngờ trùng lặp", typeof(ApiResponse))]
    public async Task<IActionResult> DismissDuplicateAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new DismissDuplicateCommand(id), ct)).ToHttpNoContent("Đã bác bỏ cờ nghi ngờ trùng lặp.");

    [HttpPost("{id:guid}/flag")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Citizen] Gắn cờ báo cáo",
        Description = "BR-REP-033: Citizen gắn cờ báo cáo (duplicate/invalid/spam/inappropriate). " +
            "Mỗi người chỉ gắn 1 cờ/loại. Khi đủ 3 cờ khác nhau cùng loại → thông báo cho LEO xem xét.")]
    [SwaggerResponse(200, "Đã gắn cờ", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy báo cáo", typeof(ApiResponse))]
    [SwaggerResponse(409, "Bạn đã gắn cờ báo cáo này rồi", typeof(ApiResponse))]
    [SwaggerResponse(422, "Không thể gắn cờ báo cáo của chính mình", typeof(ApiResponse))]
    public async Task<IActionResult> FlagAsync(
        [FromRoute] Guid id, [FromBody] FlagReportRequest request, CancellationToken ct)
        => (await sender.Send(new FlagReportCommand(id, request.Type, request.Reason), ct))
            .ToHttpNoContent("Đã gắn cờ báo cáo.");

    // ═══════════════════════════════════════════
    // ██  CITIZEN WORKFLOW
    // ═══════════════════════════════════════════

    [HttpPut("{id:guid}/close")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(Summary = "[Citizen/Auto] Đóng báo cáo", Description = "Citizen xác nhận hài lòng hoặc hệ thống tự động đóng sau 7 ngày. Chuyển status Resolved → Closed.")]
    [SwaggerResponse(200, "Đã đóng", typeof(ApiResponse))]
    [SwaggerResponse(422, "Status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> CloseAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new CloseReportCommand(id), ct)).ToHttpNoContent("Đã đóng báo cáo.");

    [HttpPost("{id:guid}/reopen-requests")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Citizen] Gửi yêu cầu mở lại báo cáo",
        Description = "Citizen gửi yêu cầu mở lại khi chưa hài lòng. Bắt buộc lý do ≥ 20 ký tự và ≥ 1 ảnh; video tùy chọn. " +
            "Report giữ Resolved cho đến khi LEO duyệt. Không bắt buộc đánh giá trước (BR-REP-015, BR-REP-018).")]
    [SwaggerResponse(200, "Đã gửi yêu cầu", typeof(ApiResponse<Guid>))]
    [SwaggerResponse(422, "Hết lượt, quá 7 ngày, hoặc thiếu minh chứng", typeof(ApiResponse))]
    public async Task<IActionResult> RequestReopenAsync(
        [FromRoute] Guid id,
        [FromBody] RequestReopenReportRequest request,
        CancellationToken ct)
        => (await sender.Send(new RequestReopenReportCommand(
            id, request.Reason, request.ImageUrls ?? [], request.VideoUrl), ct))
            .ToHttp();

    [HttpGet("reopen-requests")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Danh sách yêu cầu mở lại",
        Description = "Queue các yêu cầu reopen của citizen trong phạm vi office LEO.")]
    [SwaggerResponse(200, "Danh sách yêu cầu", typeof(ApiResponse<GetReopenRequestsResponse>))]
    public async Task<IActionResult> GetReopenRequestsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ReopenRequestStatus? status = ReopenRequestStatus.Pending,
        CancellationToken ct = default)
        => (await sender.Send(new GetReopenRequestsQuery(page, pageSize, status), ct)).ToHttp();

    [HttpPost("{id:guid}/reopen-requests/{requestId:guid}/approve")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Duyệt yêu cầu mở lại",
        Description = "LEO xác nhận yêu cầu hợp lý. Chuyển Resolved → Reopened để phân công dọn lại.")]
    [SwaggerResponse(200, "Đã duyệt", typeof(ApiResponse))]
    public async Task<IActionResult> ApproveReopenRequestAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid requestId,
        CancellationToken ct)
        => (await sender.Send(new ApproveReopenRequestCommand(id, requestId), ct))
            .ToHttpNoContent("Đã duyệt yêu cầu mở lại.");

    [HttpPost("{id:guid}/reopen-requests/{requestId:guid}/reject")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Từ chối yêu cầu mở lại",
        Description = "LEO từ chối yêu cầu không hợp lý. Report vẫn Resolved. Lý do ≥ 20 ký tự (BR-REP-022).")]
    [SwaggerResponse(200, "Đã từ chối", typeof(ApiResponse))]
    public async Task<IActionResult> RejectReopenRequestAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid requestId,
        [FromBody] RejectReopenRequestRequest request,
        CancellationToken ct)
        => (await sender.Send(new RejectReopenRequestCommand(id, requestId, request.Reason), ct))
            .ToHttpNoContent("Đã từ chối yêu cầu mở lại.");

    [HttpPut("{id:guid}/reopen")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Deprecated] Mở lại báo cáo trực tiếp",
        Description = "Đã thay bằng POST /reopen-requests. Endpoint này trả lỗi hướng dẫn dùng API mới.")]
    [SwaggerResponse(422, "Dùng POST reopen-requests", typeof(ApiResponse))]
    public async Task<IActionResult> ReopenAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new ReopenReportCommand(id), ct)).ToHttpNoContent("Đã mở lại báo cáo.");

    /// <summary>BR-REP-017: Citizen soft-deletes their own report (Submitted only, no AI/Officer).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Citizen] Xóa báo cáo",
        Description = "Citizen xóa báo cáo của mình. Chỉ cho phép ở trạng thái Submitted, " +
            "chưa có AI phân loại hoặc Officer xác minh. Soft-delete (dữ liệu vẫn lưu).")]
    [SwaggerResponse(204, "Đã xóa")]
    [SwaggerResponse(403, "Không phải người tạo báo cáo", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy báo cáo", typeof(ApiResponse))]
    [SwaggerResponse(422, "Báo cáo đã được xác nhận, không thể xóa", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new DeleteReportCommand(id), ct)).ToHttpNoContent();

    /// <summary>BR-REP-014: Persist before image URLs after direct R2 upload.</summary>
    [HttpPost("{id:guid}/before-images")]
    [Authorize(Roles = "Cleaner,CompanyStaff,Admin")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff] Lưu URL ảnh hiện trạng (presign)",
        Description = "Team leader gửi publicUrl ảnh đã PUT lên R2 (POST /v1/media/presign purpose=Before). " +
            "Bắt buộc trước khi hoàn thành (resolve). Tối đa 5 URL thuộc CDN R2 của hệ thống.")]
    [SwaggerResponse(200, "Đã lưu", typeof(ApiResponse<UploadBeforeImagesResponse>))]
    [SwaggerResponse(422, "Assignment không InProgress hoặc không phải leader", typeof(ApiResponse))]
    public async Task<IActionResult> UploadBeforeImagesAsync(
        [FromRoute] Guid id,
        [FromBody] UploadBeforeImagesRequest request,
        CancellationToken ct)
        => (await sender.Send(new UploadBeforeImagesCommand(id, request.ImageUrls ?? []), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  INSPECTION (nested resource)
    // ═══════════════════════════════════════════

    [HttpPost("{id:guid}/inspections")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Lập hồ sơ xử phạt cho báo cáo",
        Description = "LEO lập InspectionReport (Draft) liên kết với Report đã Verified (BR-INS-001, BR-OFF-005). " +
            "Có thể gán Inspection Team ngay hoặc gán sau.")]
    [SwaggerResponse(200, "Đã tạo hồ sơ xử phạt", typeof(ApiResponse<Guid>))]
    [SwaggerResponse(404, "Không tìm thấy báo cáo", typeof(ApiResponse))]
    [SwaggerResponse(409, "Đã có hồ sơ xử phạt đang hoạt động", typeof(ApiResponse))]
    [SwaggerResponse(422, "Báo cáo chưa Verified hoặc team không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> CreateInspectionAsync(
        [FromRoute] Guid id,
        [FromBody] CreateInspectionRequest request,
        CancellationToken ct)
        => (await sender.Send(new CreateInspectionReportCommand(
            id,
            request.AssignedTeamId,
            request.ViolationDescription,
            request.ViolatorName,
            request.ViolatorAddress,
            request.ViolatorIdentity), ct))
            .ToHttp("Đã lập hồ sơ xử phạt thành công.");

    [HttpGet("{id:guid}/inspections")]
    [Authorize(Roles = "LEO,Inspector,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO/Inspector] Danh sách hồ sơ xử phạt của báo cáo",
        Description = "Trả về tất cả InspectionReport liên kết với Report này.")]
    [SwaggerResponse(200, "Danh sách hồ sơ", typeof(ApiResponse<GetInspectionsByReportResponse>))]
    [SwaggerResponse(404, "Không tìm thấy báo cáo", typeof(ApiResponse))]
    public async Task<IActionResult> GetInspectionsAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetInspectionsByReportQuery(id), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  CITIZEN SATISFACTION (BR-REP-018)
    // ═══════════════════════════════════════════

    [HttpPost("{id:guid}/rate")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Citizen] Đánh giá chất lượng xử lý báo cáo",
        Description = "Citizen đánh giá mức độ hài lòng đối với báo cáo sau khi đã được Resolved hoặc Closed (BR-REP-018).")]
    [ProducesResponseType(typeof(RateReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RateAsync(
        [FromRoute] Guid id,
        [FromBody] RateReportRequest body,
        CancellationToken ct)
        => (await sender.Send(
            new RateReportCommand(id, body.IsSatisfied, body.Rating, body.Comment), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  DRAFT MANAGEMENT (BR-REP-019)
    // ═══════════════════════════════════════════

    [HttpPost("drafts")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Citizen] Tạo hoặc cập nhật bản nháp báo cáo",
        Description = "Lưu bản nháp của báo cáo ô nhiễm môi trường. Giới hạn tối đa 3 bản nháp đồng thời cho mỗi tài khoản người dùng (BR-REP-019).")]
    [ProducesResponseType(typeof(SaveDraftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SaveDraftAsync(
        [FromBody] SaveDraftRequest body,
        CancellationToken ct)
        => (await sender.Send(
            new SaveDraftCommand(body.DraftId, body.Payload), ct)).ToHttp();

    [HttpGet("drafts")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Citizen] Lấy danh sách các bản nháp cá nhân",
        Description = "Trả về danh sách các bản nháp báo cáo đang hoạt động của người dùng.")]
    [ProducesResponseType(typeof(GetMyDraftsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDraftsAsync(CancellationToken ct)
        => (await sender.Send(new GetMyDraftsQuery(), ct)).ToHttp();

    [HttpDelete("drafts/{draftId:guid}")]
    [Authorize]
    [Tags("📋 Reports — Citizen Flow")]
    [SwaggerOperation(
        Summary = "[Citizen] Xóa bản nháp",
        Description = "Xóa một bản nháp báo cáo ô nhiễm cụ thể bằng ID.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDraftAsync(
        [FromRoute] Guid draftId,
        CancellationToken ct)
        => (await sender.Send(new DeleteDraftCommand(draftId), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  OFFICER KPI (BR-OFF-021)
    // ═══════════════════════════════════════════

    /// <summary>KPI Officer: tỉ lệ xác minh đúng hạn, resolved rate, avg response time.</summary>
    [HttpGet("officer-kpi")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [Tags("📊 Officer Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO/DEO/Admin] KPI Officer",
        Description = "LEO xem KPI của mình. DEO/Admin truyền officerId để xem KPI của bất kỳ officer. " +
            "Hỗ trợ custom From/To hoặc preset period (ThisMonth, LastQuarter, etc).")]
    [SwaggerResponse(200, "KPI data", typeof(ApiResponse<OfficerKpiResponse>))]
    public async Task<IActionResult> GetOfficerKpiAsync(
        [FromQuery] Guid? officerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] KpiPeriod? period,
        CancellationToken ct)
        => (await sender.Send(
            new GetOfficerKpiQuery(officerId, from, to, period), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  EXPORT (BR-OFF-022)
    // ═══════════════════════════════════════════

    /// <summary>Export báo cáo ra CSV hoặc Excel.</summary>
    [HttpGet("export")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [Tags("📊 Officer Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO/DEO/Admin] Export báo cáo",
        Description = "LEO export xã/phường; DEO export toàn tỉnh; Admin export all. PII chỉ Admin thấy.")]
    [SwaggerResponse(200, "File download")]
    public async Task<IActionResult> ExportAsync(
        [FromQuery] ReportStatus? status,
        [FromQuery] Severity? severity,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] ExportFormat format,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new ExportReportsQuery(status, severity, from, to, format), ct);

        if (!result.IsSuccess)
            return result.ToHttp();

        var data = result.Value!;
        return File(data.Content, data.ContentType, data.FileName);
    }
}

// ── Request DTOs ──
public sealed record VerifyReportRequest(Severity? OverrideSeverity = null, Guid? OverrideCategoryId = null, List<Guid>? WasteTagIds = null);
public sealed record RejectReportRequest(string Reason);
public sealed record AssignTeamRequest(List<AssignTeamItemRequest> Teams, List<Guid>? WasteTagIds = null);
public sealed record AssignTeamItemRequest(Guid TeamId, string? Note);
public sealed record ReassignTeamRequest(Guid OldTeamId, Guid NewTeamId, string Reason);
public sealed record ResolveReportRequest(List<string> AfterImageUrls);
public sealed record UpdateProgressRequest(int ProgressPercent, string? ProgressNote = null, List<string>? ImageUrls = null);
public sealed record UploadBeforeImagesRequest(List<string> ImageUrls);
public sealed record DeclineAssignmentRequest(Guid TeamId, string Reason);
public sealed record TagWasteRequest(List<Guid> WasteTagIds);
public sealed record DispatchToCompanyRequest(Guid CompanyId, string? Note);
public sealed record AssignCompanyTeamRequest(List<AssignTeamItemRequest> Teams);
public sealed record RequestReopenReportRequest(string Reason, List<string>? ImageUrls, string? VideoUrl = null);
public sealed record RejectReopenRequestRequest(string Reason);

public sealed record CreateInspectionRequest(
    Guid? AssignedTeamId,
    string? ViolationDescription,
    string? ViolatorName,
    string? ViolatorAddress,
    string? ViolatorIdentity);

public sealed record EscalateReportRequest(string Reason);
public sealed record RateReportRequest(bool IsSatisfied, int? Rating, string? Comment);
public sealed record SaveDraftRequest(Guid? DraftId, string Payload);
public sealed record ConfirmDuplicateRequest(Guid PrimaryReportId);
public sealed record FlagReportRequest(FlagType Type, string? Reason = null);
