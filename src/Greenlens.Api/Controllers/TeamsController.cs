using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.AddTeamMember;
using Greenlens.Application.Features.Organization.CreateTeam;
using Greenlens.Application.Features.Organization.GetMyTeamProfile;
using Greenlens.Application.Features.Organization.GetTeamById;
using Greenlens.Application.Features.Organization.GetTeams;
using Greenlens.Application.Features.Organization.RemoveTeamMember;
using Greenlens.Application.Features.Organization.UpdateTeam;
using Greenlens.Application.Features.Reports.AcceptAssignment;
using Greenlens.Application.Features.Reports.DeclineAssignment;
using Greenlens.Application.Features.Reports.GetMyAssignments;
using Greenlens.Application.Features.Reports.GetMyProgressHistory;
using Greenlens.Application.Features.Reports.GetMyTaskDetail;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

/// <summary>Quản lý Teams (Đội Cleanup / Inspection).</summary>
[ApiController]
[Route("v1/teams")]
[Authorize]
[Produces("application/json")]
public sealed class TeamsController(ISender sender) : ControllerBase
{
    // ═══════════════════════════════════════════
    // ██  MY TEAM INFO
    // ═══════════════════════════════════════════

    [HttpGet("my-profile")]
    [Authorize(Roles = "Cleanup,Inspector")]
    [SwaggerOperation(Summary = "[Cleanup/Inspector] Profile team của tôi", Description = "Trả về thông tin team và danh sách thành viên của team mà user hiện tại đang thuộc về.")]
    [SwaggerResponse(200, "Profile team", typeof(ApiResponse<TeamDetailResponse>))]
    [SwaggerResponse(404, "Chưa thuộc team nào", typeof(ApiResponse))]
    public async Task<IActionResult> GetMyProfileAsync(CancellationToken ct)
        => (await sender.Send(new GetMyTeamProfileQuery(), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  TASK WORKFLOW
    // ═══════════════════════════════════════════

    [HttpGet("my-tasks")]
    [Authorize(Roles = "Cleanup,Inspector,Admin")]
    [SwaggerOperation(
        Summary = "[Cleanup/Inspector] Danh sách task được giao",
        Description = "Trả về các report được assign cho team của user đang login. " +
            "Lọc theo assignmentStatus: `Assigned` (chờ xác nhận), `InProgress` (đang làm), `Completed`, `Declined`. " +
            "Không truyền status → trả tất cả.")]
    [SwaggerResponse(200, "Danh sách task", typeof(ApiResponse<GetMyAssignmentsResponse>))]
    public async Task<IActionResult> GetMyTasksAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AssignmentStatus? assignmentStatus = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetMyAssignmentsQuery(page, pageSize, assignmentStatus), ct)).ToHttp();

    [HttpGet("my-tasks/{reportId:guid}")]
    [Authorize(Roles = "Cleanup,Inspector,Admin")]
    [SwaggerOperation(
        Summary = "[Cleanup/Inspector] Chi tiết task",
        Description = "Trả về full thông tin task từ góc nhìn của team đang login: " +
            "thông tin report, ảnh gốc (before), tiến độ hiện tại, SLA, và các action có thể thực hiện " +
            "(`canDecline`, `canUpdateProgress`, `canResolve`). " +
            "Tất cả thành viên trong team đều xem được, không chỉ leader.")]
    [SwaggerResponse(200, "Chi tiết task", typeof(ApiResponse<MyTaskDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy assignment của team này", typeof(ApiResponse))]
    [SwaggerResponse(422, "User không thuộc team nào", typeof(ApiResponse))]
    public async Task<IActionResult> GetMyTaskDetailAsync([FromRoute] Guid reportId, CancellationToken ct)
        => (await sender.Send(new GetMyTaskDetailQuery(reportId), ct)).ToHttp();

    [HttpPut("my-tasks/{reportId:guid}/accept")]
    [Authorize(Roles = "Cleanup,Inspector,Admin")]
    [SwaggerOperation(
        Summary = "[Cleanup/Inspector] Chấp nhận task",
        Description = "Team leader chấp nhận task được phân công. TeamId tự động lấy từ token. " +
            "Chuyển Assignment: `Assigned → InProgress`. `StartedAt` được set tại đây.")]
    [SwaggerResponse(204, "Đã chấp nhận")]
    [SwaggerResponse(422, "Không phải leader hoặc assignment không ở trạng thái Assigned", typeof(ApiResponse))]
    public async Task<IActionResult> AcceptTaskAsync([FromRoute] Guid reportId, CancellationToken ct)
        => (await sender.Send(new AcceptAssignmentCommand(reportId), ct)).ToHttpNoContent();

    [HttpPut("my-tasks/{reportId:guid}/decline")]
    [Authorize(Roles = "Cleanup,Inspector,Admin")]
    [SwaggerOperation(
        Summary = "[Cleanup/Inspector] Từ chối task",
        Description = "Team từ chối task trong vòng 2 giờ sau khi được phân công. Yêu cầu lý do ≥ 20 ký tự. " +
            "Nếu tất cả team đều từ chối → report quay về `Verified`.")]
    [SwaggerResponse(204, "Đã từ chối")]
    [SwaggerResponse(422, "Quá 2h, lý do quá ngắn, hoặc assignment không ở trạng thái Assigned", typeof(ApiResponse))]
    public async Task<IActionResult> DeclineTaskAsync(
        [FromRoute] Guid reportId, [FromBody] DeclineTaskRequest request, CancellationToken ct)
        => (await sender.Send(new DeclineAssignmentCommand(reportId, request.TeamId, request.Reason), ct))
            .ToHttpNoContent();

    [HttpGet("my-progress")]
    [Authorize(Roles = "Cleanup,Inspector,Admin")]
    [SwaggerOperation(
        Summary = "[Cleanup/Inspector] Lịch sử tiến độ của team",
        Description = "Trả về lịch sử cập nhật tiến độ của team. TeamId tự động lấy từ token. " +
            "Chỉ Team Leader mới gọi được. Lọc theo assignmentStatus.")]
    [SwaggerResponse(200, "Lịch sử tiến độ", typeof(ApiResponse<GetMyProgressHistoryResponse>))]
    [SwaggerResponse(422, "User không phải Team Leader", typeof(ApiResponse))]
    public async Task<IActionResult> GetMyProgressAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AssignmentStatus? assignmentStatus = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetMyProgressHistoryQuery(page, pageSize, assignmentStatus), ct)).ToHttp();

    // ── Team CRUD ──

    [HttpGet]
    [Authorize(Roles = "Admin,LEO,DEO")]
    [SwaggerOperation(Summary = "[Admin/LEO/DEO] Danh sách teams", Description = "Trả về danh sách đội MT. Hỗ trợ lọc theo office, loại team, trạng thái.")]
    [SwaggerResponse(200, "Danh sách teams", typeof(ApiResponse<GetTeamsResponse>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? localOfficeId = null, [FromQuery] TeamType? teamType = null,
        [FromQuery] bool? isActive = null, CancellationToken ct = default)
        => (await sender.Send(new GetTeamsQuery(page, pageSize, localOfficeId, teamType, isActive), ct)).ToHttp();

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,LEO,DEO")]
    [SwaggerOperation(Summary = "[Admin/LEO/DEO] Chi tiết team", Description = "Trả về thông tin team kèm danh sách thành viên (tên, email, role leader).")]
    [SwaggerResponse(200, "Chi tiết team", typeof(ApiResponse<TeamDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetTeamByIdQuery(id), ct)).ToHttp();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Tạo team", Description = "Tạo đội Cleanup (dọn dẹp) hoặc Inspection (thanh tra) thuộc 1 LocalOffice.")]
    [SwaggerResponse(201, "Đã tạo", typeof(ApiResponse<CreateTeamResponse>))]
    [SwaggerResponse(404, "Office không tồn tại", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateTeamCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Cập nhật team", Description = "Cập nhật tên team.")]
    [SwaggerResponse(204, "Đã cập nhật")]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid id, [FromBody] UpdateTeamRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateTeamCommand(id, request.Name), ct)).ToHttpNoContent();

    // ── Team Members ──

    [HttpPost("{teamId:guid}/members")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Thêm thành viên vào team", Description = "Thêm user vào đội. Role Cleanup chỉ vào team Cleanup, role Inspector chỉ vào team Inspection.")]
    [SwaggerResponse(201, "Đã thêm", typeof(ApiResponse<AddTeamMemberResponse>))]
    [SwaggerResponse(409, "User đã trong team", typeof(ApiResponse))]
    public async Task<IActionResult> AddMemberAsync(
        [FromRoute] Guid teamId, [FromBody] AddTeamMemberRequest request, CancellationToken ct)
        => (await sender.Send(new AddTeamMemberCommand(teamId, request.UserId, request.IsLeader), ct)).ToHttpCreated();

    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "[Admin] Xóa thành viên khỏi team", Description = "Xóa user khỏi đội MT.")]
    [SwaggerResponse(204, "Đã xóa")]
    [SwaggerResponse(404, "Không tìm thấy thành viên", typeof(ApiResponse))]
    public async Task<IActionResult> RemoveMemberAsync(
        [FromRoute] Guid teamId, [FromRoute] Guid userId, CancellationToken ct)
        => (await sender.Send(new RemoveTeamMemberCommand(teamId, userId), ct)).ToHttpNoContent();
}

public sealed record UpdateTeamRequest(string Name);
public sealed record AddTeamMemberRequest(Guid UserId, bool IsLeader = false);
public sealed record DeclineTaskRequest(Guid TeamId, string Reason);
