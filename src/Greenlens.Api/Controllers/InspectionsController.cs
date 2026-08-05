using Greenlens.Application.Common;
using Greenlens.Api.Attributes;
using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Inspection.AcceptInspectionTask;
using Greenlens.Application.Features.Inspection.AssignInspectionTeam;
using Greenlens.Application.Features.Inspection.CloseInspection;
using Greenlens.Application.Features.Inspection.CloseNoViolation;
using Greenlens.Application.Features.Inspection.ConfirmArrival;
using Greenlens.Application.Features.Inspection.DeclineInspection;
using Greenlens.Application.Features.Inspection.GetOfficerInspectionQueue;
using Greenlens.Application.Features.Inspection.GetInspectionQueue;
using Greenlens.Application.Features.Inspection.GetInspectionReportById;
using Greenlens.Application.Features.Inspection.GetInspectionTeamKpi;
using Greenlens.Application.Features.Inspection.DeletePenaltyPayment;
using Greenlens.Application.Features.Inspection.GetPaymentHistory;
using Greenlens.Application.Features.Inspection.IssuePenalty;
using Greenlens.Application.Features.Inspection.RecordPayment;
using Greenlens.Application.Features.Inspection.SubmitFieldInvestigation;
using Greenlens.Application.Features.Inspection.UpdateInspectionChecklist;
using Greenlens.Application.Features.Inspection.UpdateInspectionDetails;
using Greenlens.Application.Features.Inspection.UploadInspectionEvidence;
using Greenlens.Application.Features.Reports.GetOfficerKpi;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Quản lý InspectionReport — luồng xử phạt (BR-INS-*).</summary>
[ApiController]
[Route("v1/inspections")]
[Produces("application/json")]
public sealed class InspectionsController(ISender sender) : ControllerBase
{
    // ═══════════════════════════════════════════
    // ██  INSPECTOR QUEUE
    // ═══════════════════════════════════════════

    [HttpGet("queue")]
    [Authorize(Roles = "Inspector,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Danh sách hồ sơ xử phạt",
        Description = "Inspector xem danh sách InspectionReport được gán cho team của mình. " +
            "Hỗ trợ lọc theo status. Sắp xếp theo ngày tạo (mới nhất trước).")]
    [SwaggerResponse(200, "Danh sách hồ sơ", typeof(ApiResponse<GetInspectionQueueResponse>))]
    public async Task<IActionResult> GetQueueAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] InspectionStatus? status = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetInspectionQueueQuery(page, pageSize, status), ct)).ToHttp();

    [HttpGet("officer-queue")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO/DEO] Hàng đợi hồ sơ xử phạt",
        Description = "Danh sách InspectionReport trong phạm vi phường (LEO) hoặc sở (DEO). " +
            "Hỗ trợ lọc status, team, chưa gán team, SLA breach, khoảng ngày, tìm kiếm (mã báo cáo, địa chỉ, tên đối tượng). " +
            "Sắp xếp mặc định: createdAt desc.")]
    [SwaggerResponse(200, "Danh sách hồ sơ xử phạt", typeof(ApiResponse<GetOfficerInspectionQueueResponse>))]
    public async Task<IActionResult> GetOfficerQueueAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] InspectionStatus? status = null,
        [FromQuery] Guid? assignedTeamId = null,
        [FromQuery] bool? unassignedOnly = null,
        [FromQuery] bool? slaBreached = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null,
        [FromQuery] OfficerInspectionQueueSortBy sortBy = OfficerInspectionQueueSortBy.CreatedAt,
        [FromQuery] SortDirection sortDir = SortDirection.Desc,
        CancellationToken ct = default)
        => (await sender.Send(new GetOfficerInspectionQueueQuery(
            page,
            pageSize,
            status,
            assignedTeamId,
            unassignedOnly,
            slaBreached,
            fromDate,
            toDate,
            search,
            sortBy,
            sortDir), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  INSPECTION DETAIL
    // ═══════════════════════════════════════════

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Inspector,LEO,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector/LEO] Chi tiết hồ sơ xử phạt",
        Description = "Xem toàn bộ thông tin InspectionReport: vi phạm, mức phạt, SLA, trạng thái nộp phạt, " +
            "tọa độ GPS báo cáo gốc (latitude/longitude).")]
    [SwaggerResponse(200, "Chi tiết hồ sơ", typeof(ApiResponse<InspectionReportDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetInspectionReportByIdQuery(id), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  ASSIGN TEAM
    // ═══════════════════════════════════════════

    [HttpPut("{id:guid}/assign-team")]
    [Authorize(Roles = "LEO,Admin")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Gán Inspector Team cho hồ sơ xử phạt",
        Description = "LEO gán một Inspection Team cho hồ sơ đang ở trạng thái Draft. " +
            "Nếu Report gốc vẫn đang Verified, hệ thống tự động chuyển sang InProgress.")]
    [SwaggerResponse(204, "Đã gán team")]
    [SwaggerResponse(404, "Không tìm thấy hồ sơ hoặc team", typeof(ApiResponse))]
    [SwaggerResponse(422, "Team không hợp lệ hoặc status không cho phép", typeof(ApiResponse))]
    public async Task<IActionResult> AssignTeamAsync(
        [FromRoute] Guid id,
        [FromBody] AssignInspectionTeamRequest request,
        CancellationToken ct)
        => (await sender.Send(new AssignInspectionTeamCommand(id, request.TeamId), ct))
            .ToHttpNoContent("Đã gán Inspector Team thành công.");

    // ═══════════════════════════════════════════
    // ██  INSPECTION WORKFLOW
    // ═══════════════════════════════════════════

    [HttpPut("{id:guid}/details")]
    [Authorize(Roles = "Inspector,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Cập nhật biên bản hiện trường",
        Description = "Inspector cập nhật thông tin vi phạm sau khi điều tra hiện trường (BR-INS-010). " +
            "Chỉ có thể cập nhật khi hồ sơ ở trạng thái Draft.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(422, "Status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateDetailsAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateInspectionDetailsRequest request,
        CancellationToken ct)
        => (await sender.Send(new UpdateInspectionDetailsCommand(
            id,
            request.ViolationDescription,
            request.ViolatorName,
            request.ViolatorAddress,
            request.ViolatorIdentity,
            request.ViolatingEntityId), ct))
            .ToHttpNoContent("Đã cập nhật biên bản hiện trường.");

    // ═══════════════════════════════════════════
    // ██  BR-INS-033: CHECKLIST WORKFLOW
    // ═══════════════════════════════════════════

    [HttpPost("{id:guid}/accept")]
    [SupportsIdempotency]
    [Authorize(Roles = "Inspector")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Nhận task điều tra",
        Description = "Inspector Team nhận hồ sơ được gán. Draft → InProgress (BR-INS-033).")]
    [SwaggerResponse(200, "Đã nhận task", typeof(ApiResponse))]
    public async Task<IActionResult> AcceptTaskAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new AcceptInspectionTaskCommand(id), ct))
            .ToHttpNoContent("Đã nhận task điều tra.");

    [HttpPost("{id:guid}/confirm-arrival")]
    [SupportsIdempotency]
    [Authorize(Roles = "Inspector")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Xác nhận có mặt hiện trường",
        Description = "GPS mềm: ≤200m OK; >200m cần ghi chú giải trình (BR-INS-033).")]
    [SwaggerResponse(200, "Đã xác nhận", typeof(ApiResponse))]
    public async Task<IActionResult> ConfirmArrivalAsync(
        [FromRoute] Guid id,
        [FromBody] ConfirmArrivalRequest request,
        CancellationToken ct)
        => (await sender.Send(new ConfirmArrivalCommand(
            id, request.Latitude, request.Longitude, request.Note), ct))
            .ToHttpNoContent("Đã xác nhận có mặt hiện trường.");

    [HttpPut("{id:guid}/checklist")]
    [Authorize(Roles = "Inspector")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Cập nhật checklist text",
        Description = "Cập nhật mô tả tình trạng vi phạm và ghi chú khác trên checklist (BR-INS-033).")]
    [SwaggerResponse(200, "Đã cập nhật checklist", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateChecklistAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateInspectionChecklistRequest request,
        CancellationToken ct)
        => (await sender.Send(new UpdateInspectionChecklistCommand(
            id, request.ViolationStatusText, request.OtherDescription), ct))
            .ToHttpNoContent("Đã cập nhật checklist.");

    [HttpPut("{id:guid}/submit-field-report")]
    [SupportsIdempotency]
    [Authorize(Roles = "Inspector")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector Team Leader] Nộp biên bản điều tra",
        Description = "Team Leader khóa checklist và mở các hành động kết luận (BR-INS-033).")]
    [SwaggerResponse(200, "Đã nộp biên bản", typeof(ApiResponse))]
    public async Task<IActionResult> SubmitFieldReportAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new SubmitFieldInvestigationCommand(id), ct))
            .ToHttpNoContent("Đã nộp biên bản điều tra hiện trường.");

    // ═══════════════════════════════════════════
    // ██  BR-INS-033: UPLOAD CHECKLIST EVIDENCE
    // ═══════════════════════════════════════════

    [HttpPost("{id:guid}/evidence")]
    [Authorize(Roles = "Inspector")]
    [Tags("🔍 Inspection Dashboard")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "[Inspector] Upload bằng chứng checklist",
        Description = "Upload ảnh/video/audio theo category checklist (BR-INS-033). ScenePhoto cần ≥ 2 ảnh.")]
    [SwaggerResponse(200, "Đã upload", typeof(ApiResponse<UploadInspectionEvidenceResponse>))]
    public async Task<IActionResult> UploadEvidenceAsync(
        [FromRoute] Guid id,
        [FromForm] InspectionEvidenceCategory category,
        List<IFormFile> files,
        [FromForm] string? description = null,
        CancellationToken ct = default)
        => await UploadEvidenceInternalAsync(id, category, files, description, ct);

    [HttpPost("{id:guid}/evidence-images")]
    [Authorize(Roles = "Inspector")]
    [Tags("🔍 Inspection Dashboard")]
    [Consumes("multipart/form-data")]
    [Obsolete("Use POST /evidence with category=ScenePhoto")]
    [SwaggerOperation(
        Summary = "[Inspector] Upload ảnh hiện trường (legacy route)",
        Description = "Alias của POST /evidence?category=ScenePhoto.")]
    [SwaggerResponse(200, "Đã upload", typeof(ApiResponse<UploadInspectionEvidenceResponse>))]
    public async Task<IActionResult> UploadEvidenceImagesAsync(
        [FromRoute] Guid id,
        List<IFormFile> images,
        CancellationToken ct)
        => await UploadEvidenceInternalAsync(id, InspectionEvidenceCategory.ScenePhoto, images, null, ct);

    private async Task<IActionResult> UploadEvidenceInternalAsync(
        Guid id,
        InspectionEvidenceCategory category,
        List<IFormFile>? files,
        string? description,
        CancellationToken ct)
    {
        if (files is null || files.Count == 0)
        {
            return BadRequest(new ApiResponse
            {
                Code = "FILE_REQUIRED",
                Message = "Vui lòng chọn ít nhất 1 file.",
                Status = 400
            });
        }

        var evidenceFiles = new List<InspectionEvidenceFile>();
        foreach (var file in files)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            evidenceFiles.Add(new InspectionEvidenceFile(ms.ToArray(), file.FileName, file.ContentType));
        }

        var command = new UploadInspectionEvidenceCommand(id, category, evidenceFiles, description);
        return (await sender.Send(command, ct)).ToHttp();
    }

    [HttpPut("{id:guid}/issue-penalty")]
    [Authorize(Roles = "Inspector,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector Team Leader] Ban hành quyết định xử phạt",
        Description = "Team Leader ban hành QĐ xử phạt (BR-INS-012). " +
            "Tự động kiểm tra tái phạm (BR-INS-022). " +
            "Chuyển Draft → PenaltyIssued.")]
    [SwaggerResponse(200, "Đã ban hành QĐ", typeof(ApiResponse))]
    [SwaggerResponse(422, "Status không hợp lệ hoặc dữ liệu không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> IssuePenaltyAsync(
        [FromRoute] Guid id,
        [FromBody] IssuePenaltyRequest request,
        CancellationToken ct)
        => (await sender.Send(new IssuePenaltyCommand(
            id,
            request.ViolationLevel,
            request.PenaltyAmount,
            request.DecisionNumber,
            request.PaymentDueDays,
            request.AdditionalMeasures), ct))
            .ToHttpNoContent("Đã ban hành quyết định xử phạt.");

    [HttpPut("{id:guid}/close-no-violation")]
    [Authorize(Roles = "Inspector,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Đóng hồ sơ — không đủ căn cứ",
        Description = "Inspector đóng hồ sơ khi không tìm thấy vi phạm (BR-INS-013). " +
            "Lý do tối thiểu 50 ký tự. Citizen được thông báo.")]
    [SwaggerResponse(200, "Đã đóng hồ sơ", typeof(ApiResponse))]
    [SwaggerResponse(422, "Lý do quá ngắn hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> CloseNoViolationAsync(
        [FromRoute] Guid id,
        [FromBody] CloseNoViolationRequest request,
        CancellationToken ct)
        => (await sender.Send(new CloseNoViolationCommand(id, request.Reason), ct))
            .ToHttpNoContent("Đã đóng hồ sơ — không đủ căn cứ vi phạm.");

    [HttpPut("{id:guid}/record-payment")]
    [Authorize(Roles = "Inspector,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "[Inspector] Ghi nhận nộp phạt",
        Description = "Inspector ghi nhận khoản nộp phạt kèm ảnh biên lai (BR-INS-020).")]
    [SwaggerResponse(200, "Đã ghi nhận", typeof(ApiResponse))]
    public async Task<IActionResult> RecordPaymentAsync(
        [FromRoute] Guid id,
        [FromForm] decimal paidAmount,
        [FromForm] DateTime paidAt,
        IFormFile receipt,
        [FromForm] string? note = null,
        CancellationToken ct = default)
    {
        if (receipt is null || receipt.Length == 0)
        {
            return BadRequest(new ApiResponse
            {
                Code = "PAYMENT_RECEIPT_REQUIRED",
                Message = "Vui lòng upload ảnh biên lai nộp phạt.",
                Status = 400
            });
        }

        using var ms = new MemoryStream();
        await receipt.CopyToAsync(ms, ct);

        return (await sender.Send(new RecordPaymentCommand(
            id,
            paidAmount,
            paidAt,
            Note: note,
            ReceiptBytes: ms.ToArray(),
            ReceiptFileName: receipt.FileName,
            ReceiptContentType: receipt.ContentType), ct))
            .ToHttpNoContent("Đã ghi nhận nộp phạt.");
    }

    [HttpGet("{id:guid}/payments")]
    [Authorize(Roles = "Inspector,LEO,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector/LEO] Lịch sử nộp phạt",
        Description = "Xem toàn bộ lịch sử các lần nộp phạt của hồ sơ (BR-INS-020). " +
            "Bao gồm tổng tiền phạt, đã nộp, còn lại, và từng lần nộp.")]
    [SwaggerResponse(200, "Lịch sử nộp phạt", typeof(ApiResponse<GetPaymentHistoryResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetPaymentsAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetPaymentHistoryQuery(id), ct)).ToHttp();

    [HttpDelete("payments/{paymentId:guid}")]
    [Authorize(Roles = "Inspector,LEO,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector/LEO] Xóa khoản nộp phạt (Soft Delete)",
        Description = "Xóa mềm khoản thanh toán nộp phạt. Tự động tính toán lại số tiền đã đóng và có thể đẩy hồ sơ về lại trạng thái chưa đóng đủ.")]
    [SwaggerResponse(200, "Đã xóa", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy thanh toán", typeof(ApiResponse))]
    public async Task<IActionResult> DeletePaymentAsync(
        [FromRoute] Guid paymentId, CancellationToken ct)
        => (await sender.Send(new DeletePenaltyPaymentCommand(paymentId), ct)).ToHttpNoContent("Đã xóa khoản nộp phạt.");

    [HttpPut("{id:guid}/close")]
    [Authorize(Roles = "Inspector,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Đóng hồ sơ sau khi nộp phạt đầy đủ",
        Description = "Đóng hồ sơ xử phạt sau khi vi phạm đã nộp phạt đầy đủ (Paid → Closed).")]
    [SwaggerResponse(200, "Đã đóng", typeof(ApiResponse))]
    [SwaggerResponse(422, "Status không phải Paid", typeof(ApiResponse))]
    public async Task<IActionResult> CloseAsync(
        [FromRoute] Guid id,
        [FromBody] CloseInspectionRequest? request,
        CancellationToken ct)
        => (await sender.Send(new CloseInspectionCommand(id, request?.Reason), ct))
            .ToHttpNoContent("Đã đóng hồ sơ xử phạt.");

    // ═══════════════════════════════════════════
    // ██  BR-INS-003: DECLINE TASK
    // ═══════════════════════════════════════════

    [HttpPost("{id:guid}/decline")]
    [Authorize(Roles = "Inspector")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Từ chối hồ sơ xử phạt",
        Description = "Inspector từ chối trong 24h sau khi được gán (BR-INS-003). " +
            "Sau 24h coi như chấp nhận. Hồ sơ quay về Draft để LEO gán team khác.")]
    [SwaggerResponse(200, "Đã từ chối", typeof(ApiResponse))]
    [SwaggerResponse(422, "Quá hạn 24h hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> DeclineAsync(
        [FromRoute] Guid id,
        [FromBody] DeclineInspectionRequest request,
        CancellationToken ct)
        => (await sender.Send(new DeclineInspectionCommand(id, request.Reason), ct))
            .ToHttpNoContent("Đã từ chối hồ sơ xử phạt.");

    // ═══════════════════════════════════════════
    // ██  BR-INS-004: CHECK-IN (PostGIS)
    // ═══════════════════════════════════════════

    [HttpPost("{id:guid}/check-in")]
    [Authorize(Roles = "Inspector")]
    [Tags("🔍 Inspection Dashboard")]
    [Obsolete("Deprecated — use POST /accept + POST /confirm-arrival")]
    [SwaggerOperation(Summary = "[Deprecated] Check-in hiện trường")]
    [SwaggerResponse(410, "Endpoint deprecated")]
    public IActionResult CheckInAsync([FromRoute] Guid id, [FromBody] CheckInInspectionRequest request)
        => DeprecatedInspectionEndpoint();

    [HttpPut("{id:guid}/progress")]
    [Authorize(Roles = "Inspector")]
    [Tags("🔍 Inspection Dashboard")]
    [Obsolete("Deprecated — use checklist workflow (BR-INS-033)")]
    [SwaggerOperation(Summary = "[Deprecated] Cập nhật tiến độ %")]
    [SwaggerResponse(410, "Endpoint deprecated")]
    public IActionResult UpdateProgressAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateInspectionProgressRequest request)
        => DeprecatedInspectionEndpoint();

    private static ObjectResult DeprecatedInspectionEndpoint()
    {
        var error = Errors.Inspections.EndpointDeprecated;
        return new ObjectResult(new ApiResponse
        {
            Code = error.Code,
            Message = error.Message,
            Status = 410,
            Data = null
        })
        { StatusCode = 410 };
    }

    // ═══════════════════════════════════════════
    // ██  BR-INS-032: KPI INSPECTION TEAM
    // ═══════════════════════════════════════════

    [HttpGet("kpi")]
    [Authorize(Roles = "Inspector,LEO,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector/LEO/Admin] KPI Inspection Team",
        Description = "Tỷ lệ ban hành QĐ đúng hạn, tỷ lệ nộp phạt đúng hạn, tái phạm, SLA breach (BR-INS-032). " +
            "Inspector xem team mình. LEO/Admin có thể chỉ định teamId.")]
    [SwaggerResponse(200, "KPI data", typeof(ApiResponse<InspectionTeamKpiResponse>))]
    public async Task<IActionResult> GetKpiAsync(
        [FromQuery] Guid? teamId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] KpiPeriod? period = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetInspectionTeamKpiQuery(teamId, from, to, period), ct)).ToHttp();
}

// ── Request DTOs ──
public sealed record UpdateInspectionDetailsRequest(
    string? ViolationDescription,
    string? ViolatorName,
    string? ViolatorAddress,
    string? ViolatorIdentity,
    Guid? ViolatingEntityId = null);

public sealed record IssuePenaltyRequest(
    ViolationLevel ViolationLevel,
    decimal PenaltyAmount,
    string DecisionNumber,
    int PaymentDueDays = 10,
    string? AdditionalMeasures = null);

public sealed record CloseNoViolationRequest(string Reason);

public sealed record RecordPaymentRequest(
    decimal PaidAmount,
    DateTime PaidAt,
    string? EvidenceUrl = null,
    string? Note = null);

public sealed record CloseInspectionRequest(string? Reason);

public sealed record DeclineInspectionRequest(string Reason);

public sealed record ConfirmArrivalRequest(
    decimal Latitude,
    decimal Longitude,
    string? Note = null);

public sealed record UpdateInspectionChecklistRequest(
    string ViolationStatusText,
    string? OtherDescription = null);

public sealed record CheckInInspectionRequest(
    decimal Latitude,
    decimal Longitude,
    string? Note = null);

public sealed record UpdateInspectionProgressRequest(
    int Percent,
    string? Note = null);

public sealed record AssignInspectionTeamRequest(Guid TeamId);
