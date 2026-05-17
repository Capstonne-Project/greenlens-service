using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.AssignTeam;
using Greenlens.Application.Features.Reports.CloseNoViolation;
using Greenlens.Application.Features.Reports.CloseReport;
using Greenlens.Application.Features.Reports.DeclineAssignment;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Application.Features.Reports.IssuePenalty;
using Greenlens.Application.Features.Reports.ReassignTeam;
using Greenlens.Application.Features.Reports.RejectReport;
using Greenlens.Application.Features.Reports.ReopenReport;
using Greenlens.Application.Features.Reports.ResolveReport;
using Greenlens.Application.Features.Reports.VerifyReport;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>
/// Report lifecycle endpoints: verification, assignment, resolve, penalty, close.
/// </summary>
[ApiController]
[Route("v1/reports")]
[Authorize]
[Produces("application/json")]
public sealed class ReportWorkflowController(ISender sender) : ControllerBase
{
    // ── Officer: Verify / Reject ──

    [HttpPut("{id:guid}/verify")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [SwaggerOperation(Summary = "[LEO/DEO] Xác minh báo cáo", Description = "Officer kiểm tra thông tin và xác minh báo cáo. Có thể override severity và category nếu cần. Chuyển status Submitted → Verified.")]
    [SwaggerResponse(204, "Report verified")]
    [SwaggerResponse(404, "Report not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid status transition or conflict of interest", typeof(ApiResponse))]
    public async Task<IActionResult> VerifyAsync(
        [FromRoute] Guid id,
        [FromBody] VerifyReportRequest request,
        CancellationToken ct)
        => (await sender.Send(new VerifyReportCommand(id, request.OverrideSeverity, request.OverrideCategoryId), ct))
            .ToHttpNoContent();

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [SwaggerOperation(Summary = "[LEO/DEO] Từ chối báo cáo", Description = "Officer từ chối báo cáo không hợp lệ. Yêu cầu lý do ≥ 20 ký tự. Chuyển status Submitted → Rejected.")]
    [SwaggerResponse(204, "Report rejected")]
    [SwaggerResponse(404, "Report not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Reason too short or invalid status", typeof(ApiResponse))]
    public async Task<IActionResult> RejectAsync(
        [FromRoute] Guid id,
        [FromBody] RejectReportRequest request,
        CancellationToken ct)
        => (await sender.Send(new RejectReportCommand(id, request.Reason), ct))
            .ToHttpNoContent();

    // ── Officer: Assign / Reassign Team ──

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [SwaggerOperation(Summary = "[LEO/DEO] Phân công team xử lý", Description = "Phân công 1 hoặc nhiều team cùng xử lý báo cáo đã xác minh. Tất cả team ngang hàng, mỗi team có status riêng. Team type phải khớp loại ô nhiễm, mỗi team tối đa 10 task In-Progress. Chuyển status Verified → InProgress.")]
    [SwaggerResponse(204, "Teams assigned")]
    [SwaggerResponse(404, "Report or team not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Team type mismatch or workload exceeded", typeof(ApiResponse))]
    public async Task<IActionResult> AssignTeamAsync(
        [FromRoute] Guid id,
        [FromBody] AssignTeamRequest request,
        CancellationToken ct)
    {
        var items = request.Teams
            .Select(t => new TeamAssignmentItem(t.TeamId, t.Note))
            .ToList();
        return (await sender.Send(new AssignTeamCommand(id, items), ct)).ToHttpNoContent();
    }

    [HttpPut("{id:guid}/reassign")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [SwaggerOperation(Summary = "[LEO/DEO] Chuyển giao team", Description = "Chuyển assignment từ team cũ sang team mới cùng loại (Cleanup↔Cleanup, Inspection↔Inspection). Yêu cầu lý do ≥ 20 ký tự.")]
    [SwaggerResponse(204, "Reassigned")]
    [SwaggerResponse(422, "Different team types or workload exceeded", typeof(ApiResponse))]
    public async Task<IActionResult> ReassignAsync(
        [FromRoute] Guid id,
        [FromBody] ReassignTeamRequest request,
        CancellationToken ct)
        => (await sender.Send(new ReassignTeamCommand(id, request.OldTeamId, request.NewTeamId, request.Reason), ct))
            .ToHttpNoContent();

    // ── Cleanup Team: Resolve ──

    [HttpPut("{id:guid}/resolve")]
    [Authorize(Roles = "Cleanup,Admin")]
    [SwaggerOperation(Summary = "[Cleanup] Hoàn thành phần việc của team", Description = "Cleanup Team đánh dấu phần việc đã hoàn thành. Yêu cầu ≥ 2 ảnh after. Khi tất cả team đều completed → report chuyển InProgress → Resolved.")]
    [SwaggerResponse(204, "Resolved")]
    [SwaggerResponse(422, "Insufficient images or invalid status", typeof(ApiResponse))]
    public async Task<IActionResult> ResolveAsync(
        [FromRoute] Guid id,
        [FromBody] ResolveReportRequest request,
        CancellationToken ct)
        => (await sender.Send(new ResolveReportCommand(id, request.TeamId, request.AfterImageUrls), ct))
            .ToHttpNoContent();

    // ── Inspection Team: Penalty / No Violation ──

    [HttpPut("{id:guid}/penalty")]
    [Authorize(Roles = "Inspector,Admin")]
    [SwaggerOperation(Summary = "[Inspector] Xử phạt vi phạm", Description = "Inspection Team Leader ban hành quyết định xử phạt. Khi tất cả team đều completed → report chuyển InProgress → PenaltyIssued.")]
    [SwaggerResponse(204, "Penalty issued")]
    [SwaggerResponse(422, "Invalid status", typeof(ApiResponse))]
    public async Task<IActionResult> IssuePenaltyAsync(
        [FromRoute] Guid id,
        [FromBody] IssuePenaltyRequest request,
        CancellationToken ct)
        => (await sender.Send(new IssuePenaltyCommand(id, request.TeamId), ct)).ToHttpNoContent();

    [HttpPut("{id:guid}/close-no-violation")]
    [Authorize(Roles = "Inspector,Admin")]
    [SwaggerOperation(Summary = "[Inspector] Đóng — không vi phạm", Description = "Inspection Team đóng báo cáo khi khảo sát không phát hiện vi phạm. Yêu cầu lý do ≥ 50 ký tự. Chuyển status InProgress → ClosedNoViolation.")]
    [SwaggerResponse(204, "Closed")]
    [SwaggerResponse(422, "Reason too short or invalid status", typeof(ApiResponse))]
    public async Task<IActionResult> CloseNoViolationAsync(
        [FromRoute] Guid id,
        [FromBody] CloseNoViolationRequest request,
        CancellationToken ct)
        => (await sender.Send(new CloseNoViolationCommand(id, request.Reason), ct))
            .ToHttpNoContent();

    // ── Citizen / Auto: Close / Reopen ──

    [HttpPut("{id:guid}/close")]
    [SwaggerOperation(Summary = "[Citizen/Auto] Đóng báo cáo", Description = "Citizen xác nhận hài lòng hoặc hệ thống tự động đóng sau 7 ngày. Chuyển status Resolved/PenaltyIssued → Closed.")]
    [SwaggerResponse(204, "Closed")]
    [SwaggerResponse(422, "Invalid status", typeof(ApiResponse))]
    public async Task<IActionResult> CloseAsync(
        [FromRoute] Guid id,
        CancellationToken ct)
        => (await sender.Send(new CloseReportCommand(id), ct)).ToHttpNoContent();

    [HttpPut("{id:guid}/reopen")]
    [SwaggerOperation(Summary = "[Citizen] Mở lại báo cáo", Description = "Citizen mở lại báo cáo nếu chưa hài lòng với kết quả. Tối đa 2 lần reopen. Chuyển status Resolved → InProgress.")]
    [SwaggerResponse(204, "Reopened")]
    [SwaggerResponse(422, "Reopen limit reached or invalid status", typeof(ApiResponse))]
    public async Task<IActionResult> ReopenAsync(
        [FromRoute] Guid id,
        CancellationToken ct)
        => (await sender.Send(new ReopenReportCommand(id), ct)).ToHttpNoContent();

    // ── Team: Decline ──

    [HttpPut("{id:guid}/decline")]
    [Authorize(Roles = "Cleanup,Inspector,Admin")]
    [SwaggerOperation(Summary = "[Cleanup/Inspector] Từ chối task", Description = "Team từ chối task trong vòng 2 giờ sau khi được phân công. Yêu cầu lý do ≥ 20 ký tự. Quá 2h không thể từ chối.")]
    [SwaggerResponse(204, "Declined")]
    [SwaggerResponse(422, "Decline window expired or reason too short", typeof(ApiResponse))]
    public async Task<IActionResult> DeclineAsync(
        [FromRoute] Guid id,
        [FromBody] DeclineAssignmentRequest request,
        CancellationToken ct)
        => (await sender.Send(new DeclineAssignmentCommand(id, request.TeamId, request.Reason), ct))
            .ToHttpNoContent();

    // ── Officer: Queue ──

    [HttpGet("queue")]
    [Authorize(Roles = "LEO,DEO,Admin")]
    [SwaggerOperation(Summary = "[LEO/DEO] Xem hàng đợi báo cáo", Description = "Trả về danh sách báo cáo trong phạm vi quản lý, sắp theo điểm ưu tiên giảm dần. LEO thấy báo cáo trong xã/phường, DEO thấy toàn tỉnh.")]
    [SwaggerResponse(200, "Queue returned", typeof(ApiResponse<GetOfficerQueueResponse>))]
    public async Task<IActionResult> GetQueueAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ReportStatus? status = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetOfficerQueueQuery(page, pageSize, status), ct)).ToHttp();
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
