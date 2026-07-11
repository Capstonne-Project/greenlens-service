using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Inspection.CloseInspection;
using Greenlens.Application.Features.Inspection.CloseNoViolation;
using Greenlens.Application.Features.Inspection.CheckInInspection;
using Greenlens.Application.Features.Inspection.DeclineInspection;
using Greenlens.Application.Features.Inspection.GetInspectionQueue;
using Greenlens.Application.Features.Inspection.GetInspectionReportById;
using Greenlens.Application.Features.Inspection.GetInspectionTeamKpi;
using Greenlens.Application.Features.Inspection.IssuePenalty;
using Greenlens.Application.Features.Inspection.RecordPayment;
using Greenlens.Application.Features.Inspection.UpdateInspectionDetails;
using Greenlens.Application.Features.Inspection.UpdateInspectionProgress;
using Greenlens.Application.Features.Reports.GetOfficerKpi;
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

    // ═══════════════════════════════════════════
    // ██  INSPECTION DETAIL
    // ═══════════════════════════════════════════

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Inspector,LEO,Admin")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector/LEO] Chi tiết hồ sơ xử phạt",
        Description = "Xem toàn bộ thông tin InspectionReport: vi phạm, mức phạt, SLA, trạng thái nộp phạt.")]
    [SwaggerResponse(200, "Chi tiết hồ sơ", typeof(ApiResponse<InspectionReportDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetInspectionReportByIdQuery(id), ct)).ToHttp();

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
            request.ViolatorIdentity), ct))
            .ToHttpNoContent("Đã cập nhật biên bản hiện trường.");

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
    [SwaggerOperation(
        Summary = "[Inspector] Ghi nhận nộp phạt",
        Description = "Inspector ghi nhận khoản nộp phạt (BR-INS-020). " +
            "Hệ thống tự tính Paid vs PartiallyPaid dựa trên tổng đã nộp.")]
    [SwaggerResponse(200, "Đã ghi nhận", typeof(ApiResponse))]
    [SwaggerResponse(422, "Status không hợp lệ hoặc số tiền <= 0", typeof(ApiResponse))]
    public async Task<IActionResult> RecordPaymentAsync(
        [FromRoute] Guid id,
        [FromBody] RecordPaymentRequest request,
        CancellationToken ct)
        => (await sender.Send(new RecordPaymentCommand(id, request.PaidAmount), ct))
            .ToHttpNoContent("Đã ghi nhận nộp phạt.");

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
    [SwaggerOperation(
        Summary = "[Inspector] Check-in hiện trường",
        Description = "Check-in tại hiện trường ≤ 200m (PostGIS, BR-INS-004). " +
            "Chuyển Draft → InProgress.")]
    [SwaggerResponse(200, "Đã check-in", typeof(ApiResponse))]
    [SwaggerResponse(422, "Quá xa hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> CheckInAsync(
        [FromRoute] Guid id,
        [FromBody] CheckInInspectionRequest request,
        CancellationToken ct)
        => (await sender.Send(new CheckInInspectionCommand(
            id, request.Latitude, request.Longitude, request.Note), ct))
            .ToHttpNoContent("Đã check-in hiện trường.");

    // ═══════════════════════════════════════════
    // ██  BR-INS-031: UPDATE PROGRESS
    // ═══════════════════════════════════════════

    [HttpPut("{id:guid}/progress")]
    [Authorize(Roles = "Inspector")]
    [Tags("🔍 Inspection Dashboard")]
    [SwaggerOperation(
        Summary = "[Inspector] Cập nhật tiến độ",
        Description = "Cập nhật tiến độ xử phạt (BR-INS-031). Yêu cầu ≥ 1 lần/ngày khi InProgress.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(422, "Status không phải InProgress", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateProgressAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateInspectionProgressRequest request,
        CancellationToken ct)
        => (await sender.Send(new UpdateInspectionProgressCommand(
            id, request.Percent, request.Note), ct))
            .ToHttpNoContent("Đã cập nhật tiến độ.");

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
    string? ViolatorIdentity);

public sealed record IssuePenaltyRequest(
    ViolationLevel ViolationLevel,
    decimal PenaltyAmount,
    string DecisionNumber,
    int PaymentDueDays = 10,
    string? AdditionalMeasures = null);

public sealed record CloseNoViolationRequest(string Reason);

public sealed record RecordPaymentRequest(decimal PaidAmount);

public sealed record CloseInspectionRequest(string? Reason);

public sealed record DeclineInspectionRequest(string Reason);

public sealed record CheckInInspectionRequest(
    decimal Latitude,
    decimal Longitude,
    string? Note = null);

public sealed record UpdateInspectionProgressRequest(
    int Percent,
    string? Note = null);
