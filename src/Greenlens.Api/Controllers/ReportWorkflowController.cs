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
    [SwaggerOperation(Summary = "Verify report", Description = "LEO/DEO verifies a submitted report (BR-OFF-001, BR-REP-020).")]
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
    [SwaggerOperation(Summary = "Reject report", Description = "LEO/DEO rejects a submitted report (BR-REP-022). Reason ≥ 20 chars.")]
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
    [SwaggerOperation(Summary = "Assign team(s)", Description = "LEO assigns one or more teams to a verified report (BR-OFF-011, BR-ORG-013).")]
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
    [SwaggerOperation(Summary = "Reassign team", Description = "LEO reassigns to a different team of the same type (BR-OFF-012).")]
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
    [SwaggerOperation(Summary = "Resolve report", Description = "Cleanup Team marks as resolved with ≥ 2 after images (BR-CLN-005).")]
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
    [SwaggerOperation(Summary = "Issue penalty", Description = "Inspection Team Leader issues penalty decision (BR-INS-012).")]
    [SwaggerResponse(204, "Penalty issued")]
    [SwaggerResponse(422, "Invalid status", typeof(ApiResponse))]
    public async Task<IActionResult> IssuePenaltyAsync(
        [FromRoute] Guid id,
        [FromBody] IssuePenaltyRequest request,
        CancellationToken ct)
        => (await sender.Send(new IssuePenaltyCommand(id, request.TeamId), ct)).ToHttpNoContent();

    [HttpPut("{id:guid}/close-no-violation")]
    [Authorize(Roles = "Inspector,Admin")]
    [SwaggerOperation(Summary = "Close - no violation", Description = "Inspection Team closes with no violation found (BR-INS-013). Reason ≥ 50 chars.")]
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
    [SwaggerOperation(Summary = "Close report", Description = "Citizen confirms or auto-close (BR-REP-016).")]
    [SwaggerResponse(204, "Closed")]
    [SwaggerResponse(422, "Invalid status", typeof(ApiResponse))]
    public async Task<IActionResult> CloseAsync(
        [FromRoute] Guid id,
        CancellationToken ct)
        => (await sender.Send(new CloseReportCommand(id), ct)).ToHttpNoContent();

    [HttpPut("{id:guid}/reopen")]
    [SwaggerOperation(Summary = "Reopen report", Description = "Citizen reopens if not satisfied (BR-REP-015). Max 2 times.")]
    [SwaggerResponse(204, "Reopened")]
    [SwaggerResponse(422, "Reopen limit reached or invalid status", typeof(ApiResponse))]
    public async Task<IActionResult> ReopenAsync(
        [FromRoute] Guid id,
        CancellationToken ct)
        => (await sender.Send(new ReopenReportCommand(id), ct)).ToHttpNoContent();

    // ── Team: Decline ──

    [HttpPut("{id:guid}/decline")]
    [Authorize(Roles = "Cleanup,Inspector,Admin")]
    [SwaggerOperation(Summary = "Decline assignment", Description = "Team declines task within 2h window (BR-CLN-007, BR-INS-003).")]
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
    [SwaggerOperation(Summary = "Get officer queue", Description = "Returns paginated queue sorted by priority (BR-OFF-010).")]
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
