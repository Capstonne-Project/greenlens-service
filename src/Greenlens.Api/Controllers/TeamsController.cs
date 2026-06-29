using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.AddCompanyTeamMember;
using Greenlens.Application.Features.Organization.AddTeamMember;
using Greenlens.Application.Features.Organization.CreateCompanyTeam;
using Greenlens.Application.Features.Organization.CreateTeam;
using Greenlens.Application.Features.Organization.DeleteCompanyTeam;
using Greenlens.Application.Features.Organization.GetCompanyTeams;
using Greenlens.Application.Features.Organization.GetMyTeamProfile;
using Greenlens.Application.Features.Organization.GetTeamById;
using Greenlens.Application.Features.Organization.GetTeams;
using Greenlens.Application.Features.Organization.RemoveCompanyTeamMember;
using Greenlens.Application.Features.Organization.RemoveTeamMember;
using Greenlens.Application.Features.Organization.TransferTeamMember;
using Greenlens.Application.Features.Organization.UpdateCompanyTeam;
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

/// <summary>Quản lý Teams (Đội Cleaner / Inspection) — Community + Company.</summary>
[ApiController]
[Route("v1/teams")]
[Authorize]
[Produces("application/json")]
public sealed class TeamsController(ISender sender) : ControllerBase
{
    // ═══════════════════════════════════════════
    // ██  MY TEAM INFO (Cleaner/Inspector/CompanyStaff)
    // ═══════════════════════════════════════════

    [HttpGet("my-profile")]
    [Authorize(Roles = "Cleaner,CompanyStaff,Inspector")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff/Inspector] Profile team của tôi",
        Description = "Trả về thông tin team và danh sách thành viên của team mà user hiện tại đang thuộc về. " +
            "Dùng chung cho cả community team (Cleaner) và company team (CompanyStaff).")]
    [SwaggerResponse(200, "Profile team", typeof(ApiResponse<TeamDetailResponse>))]
    [SwaggerResponse(404, "Chưa thuộc team nào", typeof(ApiResponse))]
    public async Task<IActionResult> GetMyProfileAsync(CancellationToken ct)
        => (await sender.Send(new GetMyTeamProfileQuery(), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  TASK WORKFLOW (Cleaner/Inspector/CompanyStaff)
    // ═══════════════════════════════════════════

    [HttpGet("my-tasks")]
    [Authorize(Roles = "Cleaner,CompanyStaff,Inspector,Admin")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff/Inspector] Danh sách task được giao",
        Description = "Trả về các report được assign cho team của user đang login. " +
            "Lọc theo assignmentStatus: `Assigned` (chờ xác nhận), `InProgress` (đang làm), `Completed`, `Declined`. " +
            "Không truyền status → trả tất cả. " +
            "**Dùng chung cho community team (role Cleaner, LEO assign) và company team (role CompanyStaff, CM assign).**")]
    [SwaggerResponse(200, "Danh sách task", typeof(ApiResponse<GetMyAssignmentsResponse>))]
    public async Task<IActionResult> GetMyTasksAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AssignmentStatus? assignmentStatus = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetMyAssignmentsQuery(page, pageSize, assignmentStatus), ct)).ToHttp();

    [HttpGet("my-tasks/{reportId:guid}")]
    [Authorize(Roles = "Cleaner,CompanyStaff,Inspector,Admin")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff/Inspector] Chi tiết task",
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
    [Authorize(Roles = "Cleaner,CompanyStaff,Inspector,Admin")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff/Inspector] Chấp nhận task",
        Description = "Team leader chấp nhận task được phân công. TeamId tự động lấy từ token. " +
            "Chuyển Assignment: `Assigned → InProgress`. `StartedAt` được set tại đây. " +
            "Dùng chung cho Cleaner (community) và CompanyStaff (company).")]
    [SwaggerResponse(200, "Đã chấp nhận", typeof(ApiResponse))]
    [SwaggerResponse(422, "Không phải leader hoặc assignment không ở trạng thái Assigned", typeof(ApiResponse))]
    public async Task<IActionResult> AcceptTaskAsync([FromRoute] Guid reportId, CancellationToken ct)
        => (await sender.Send(new AcceptAssignmentCommand(reportId), ct)).ToHttpNoContent("Đã chấp nhận task.");

    [HttpPut("my-tasks/{reportId:guid}/decline")]
    [Authorize(Roles = "Cleaner,CompanyStaff,Inspector,Admin")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff/Inspector] Từ chối task",
        Description = "Team từ chối task trong vòng 2 giờ sau khi được phân công. Yêu cầu lý do ≥ 20 ký tự. " +
            "Nếu tất cả team đều từ chối → report quay về `Verified` để LEO/CM phân công lại.")]
    [SwaggerResponse(200, "Đã từ chối", typeof(ApiResponse))]
    [SwaggerResponse(422, "Quá 2h, lý do quá ngắn, hoặc assignment không ở trạng thái Assigned", typeof(ApiResponse))]
    public async Task<IActionResult> DeclineTaskAsync(
        [FromRoute] Guid reportId, [FromBody] DeclineTaskRequest request, CancellationToken ct)
        => (await sender.Send(new DeclineAssignmentCommand(reportId, request.TeamId, request.Reason), ct))
            .ToHttpNoContent("Đã từ chối task.");

    [HttpGet("my-progress")]
    [Authorize(Roles = "Cleaner,CompanyStaff,Inspector,Admin")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff/Inspector] Lịch sử tiến độ của team",
        Description = "Trả về lịch sử cập nhật tiến độ của team. TeamId tự động lấy từ token. " +
            "Chỉ Team Leader mới gọi được. Lọc theo assignmentStatus. " +
            "Dùng chung cho Cleaner (community) và CompanyStaff (company).")]
    [SwaggerResponse(200, "Lịch sử tiến độ", typeof(ApiResponse<GetMyProgressHistoryResponse>))]
    [SwaggerResponse(422, "User không phải Team Leader", typeof(ApiResponse))]
    public async Task<IActionResult> GetMyProgressAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] AssignmentStatus? assignmentStatus = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetMyProgressHistoryQuery(page, pageSize, assignmentStatus), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  COMMUNITY TEAM CRUD (LEO)
    // ═══════════════════════════════════════════

    [HttpGet]
    [Authorize(Roles = "Admin,LEO,DEO")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[Admin/LEO/DEO] Danh sách teams cộng đồng",
        Description = "Trả về danh sách đội MT cộng đồng (CompanyId == null) kèm trạng thái hiện tại (Available/Busy). " +
            "Hỗ trợ lọc theo office, loại team, trạng thái và tình trạng rảnh/bận (isAvailable). " +
            "Để xem team công ty → dùng `GET /v1/teams/company-teams` (CompanyManager).")]
    [SwaggerResponse(200, "Danh sách teams", typeof(ApiResponse<GetTeamsResponse>))]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? localOfficeId = null, [FromQuery] TeamType? teamType = null,
        [FromQuery] bool? isActive = null, [FromQuery] bool? isAvailable = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetTeamsQuery(page, pageSize, localOfficeId, teamType, isActive, isAvailable), ct)).ToHttp();

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,LEO,DEO,CompanyManager")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[Admin/LEO/DEO/CM] Chi tiết team",
        Description = "Trả về thông tin team kèm danh sách thành viên (tên, email, role leader). " +
            "LEO xem team cộng đồng, CompanyManager xem team công ty mình.")]
    [SwaggerResponse(200, "Chi tiết team", typeof(ApiResponse<TeamDetailResponse>))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new GetTeamByIdQuery(id), ct)).ToHttp();

    [HttpPost]
    [Authorize(Roles = "LEO")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[LEO] Tạo team cộng đồng",
        Description = "Tạo đội Cleaner (dọn dẹp) hoặc Inspection (thanh tra). " +
            "LocalOfficeId tự resolve từ token — chỉ cần truyền name + teamType. " +
            "Để tạo team **công ty** → dùng `POST /v1/teams/company-teams` (CompanyManager).")]
    [SwaggerResponse(201, "Đã tạo", typeof(ApiResponse<CreateTeamResponse>))]
    [SwaggerResponse(404, "LEO chưa được gán office", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateTeamCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,LEO")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[Admin/LEO] Cập nhật team cộng đồng",
        Description = "Cập nhật tên team. LEO chỉ cập nhật team trong office của mình.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] Guid id, [FromBody] UpdateTeamRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateTeamCommand(id, request.Name), ct)).ToHttpNoContent("Đã cập nhật team.");

    // ── Team Members (LEO) ──

    [HttpPost("{teamId:guid}/members")]
    [Authorize(Roles = "Admin,LEO")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[Admin/LEO] Thêm thành viên vào team cộng đồng",
        Description = "Thêm user vào đội. Role Cleaner chỉ vào team Cleanup, role Inspector chỉ vào team Inspection. " +
            "LEO chỉ quản lý team trong office của mình.")]
    [SwaggerResponse(201, "Đã thêm", typeof(ApiResponse<AddTeamMemberResponse>))]
    [SwaggerResponse(409, "User đã trong team", typeof(ApiResponse))]
    public async Task<IActionResult> AddMemberAsync(
        [FromRoute] Guid teamId, [FromBody] AddTeamMemberRequest request, CancellationToken ct)
        => (await sender.Send(new AddTeamMemberCommand(teamId, request.UserId, request.IsLeader), ct)).ToHttpCreated();

    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    [Authorize(Roles = "Admin,LEO")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[Admin/LEO] Xóa thành viên khỏi team cộng đồng",
        Description = "Xóa user khỏi đội MT. LEO chỉ quản lý team trong office của mình. " +
            "Role không thay đổi — user vẫn thuộc phường.")]
    [SwaggerResponse(200, "Đã xóa", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy thành viên", typeof(ApiResponse))]
    public async Task<IActionResult> RemoveMemberAsync(
        [FromRoute] Guid teamId, [FromRoute] Guid userId, CancellationToken ct)
        => (await sender.Send(new RemoveTeamMemberCommand(teamId, userId), ct)).ToHttpNoContent("Đã xóa thành viên khỏi team.");

    [HttpPut("{teamId:guid}/members/{userId:guid}/transfer")]
    [Authorize(Roles = "Admin,LEO")]
    [Tags("📌 LEO Dashboard")]
    [SwaggerOperation(
        Summary = "[Admin/LEO] Chuyển thành viên sang team khác",
        Description = "Chuyển user từ team hiện tại sang team mới trong cùng office (atomic). " +
            "Role không thay đổi. Cả 2 team phải thuộc office của LEO. " +
            "Role phải khớp TeamType mới (Cleaner→Cleanup, Inspector→Inspection).")]
    [SwaggerResponse(200, "Đã chuyển", typeof(ApiResponse<TransferTeamMemberResponse>))]
    [SwaggerResponse(404, "Không tìm thấy team hoặc thành viên", typeof(ApiResponse))]
    [SwaggerResponse(409, "User đã trong team đích", typeof(ApiResponse))]
    [SwaggerResponse(422, "Team không thuộc office hoặc role không khớp", typeof(ApiResponse))]
    public async Task<IActionResult> TransferMemberAsync(
        [FromRoute] Guid teamId, [FromRoute] Guid userId,
        [FromBody] TransferMemberRequest request, CancellationToken ct)
        => (await sender.Send(
            new TransferTeamMemberCommand(teamId, userId, request.NewTeamId, request.IsLeader), ct)).ToHttp();

    // ═══════════════════════════════════════════
    // ██  COMPANY TEAM CRUD (CompanyManager)
    // ═══════════════════════════════════════════

    [HttpGet("company-teams")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Danh sách team của công ty",
        Description = "CompanyManager xem danh sách team thuộc công ty mình. " +
            "Chỉ trả về team có CompanyId == công ty của user. " +
            "Hỗ trợ lọc theo trạng thái active/inactive.")]
    [SwaggerResponse(200, "Danh sách team công ty", typeof(ApiResponse<GetCompanyTeamsResponse>))]
    [SwaggerResponse(403, "Không phải CompanyManager hoặc chưa gán công ty", typeof(ApiResponse))]
    public async Task<IActionResult> GetCompanyTeamsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
        => (await sender.Send(new GetCompanyTeamsQuery(page, pageSize, isActive), ct)).ToHttp();

    [HttpPost("company-teams")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Tạo team cho công ty",
        Description = "CompanyManager tạo đội CleanupTeam thuộc công ty mình. " +
            "CompanyId tự động gắn từ token (không truyền trong body). " +
            "Team công ty **không gắn cố định LocalOffice** — đi theo task được dispatch. " +
            "**InspectionTeam KHÔNG được phép** — đội xử phạt luôn thuộc phường/xã (LEO quản lý).")]
    [SwaggerResponse(201, "Đã tạo", typeof(ApiResponse<CreateCompanyTeamResponse>))]
    [SwaggerResponse(403, "Không phải CompanyManager", typeof(ApiResponse))]
    public async Task<IActionResult> CreateCompanyTeamAsync(
        [FromBody] CreateCompanyTeamCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpPut("company-teams/{id:guid}")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Đổi tên team công ty",
        Description = "CM đổi tên team thuộc công ty mình. Chỉ team có CompanyId trùng công ty của CM mới được sửa.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy team", typeof(ApiResponse))]
    [SwaggerResponse(403, "Team không thuộc công ty của bạn", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateCompanyTeamAsync(
        [FromRoute] Guid id, [FromBody] UpdateCompanyTeamRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateCompanyTeamCommand(id, request.Name), ct))
            .ToHttpNoContent("Đã cập nhật team.");

    [HttpDelete("company-teams/{id:guid}")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Vô hiệu hóa team công ty",
        Description = "CM deactivate (soft-delete) team thuộc công ty mình. " +
            "Team không bị xóa khỏi DB — chỉ set IsActive = false. " +
            "Team đã deactivate không thể nhận task mới.")]
    [SwaggerResponse(200, "Đã vô hiệu hóa", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy team", typeof(ApiResponse))]
    [SwaggerResponse(422, "Team đã bị vô hiệu hóa trước đó", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteCompanyTeamAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new DeleteCompanyTeamCommand(id), ct))
            .ToHttpNoContent("Đã vô hiệu hóa team.");

    // ── Company Team Members (CM) ──

    [HttpPost("company-teams/{teamId:guid}/members")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Thêm nhân viên vào team công ty",
        Description = "CM thêm CompanyStaff (thuộc cùng công ty) vào team. " +
            "User phải có role CompanyStaff và thuộc cùng công ty của CM. " +
            "Có thể gán làm leader.")]
    [SwaggerResponse(201, "Đã thêm", typeof(ApiResponse<AddCompanyTeamMemberResponse>))]
    [SwaggerResponse(404, "Team hoặc user không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(409, "User đã trong team", typeof(ApiResponse))]
    [SwaggerResponse(422, "User không thuộc công ty hoặc sai role", typeof(ApiResponse))]
    public async Task<IActionResult> AddCompanyTeamMemberAsync(
        [FromRoute] Guid teamId, [FromBody] AddCompanyTeamMemberRequest request, CancellationToken ct)
        => (await sender.Send(new AddCompanyTeamMemberCommand(teamId, request.UserId, request.IsLeader), ct)).ToHttpCreated();

    [HttpDelete("company-teams/{teamId:guid}/members/{userId:guid}")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Xóa nhân viên khỏi team công ty",
        Description = "CM xóa CompanyStaff khỏi team. " +
            "User vẫn thuộc công ty (CompanyStaff record không thay đổi), chỉ rời team.")]
    [SwaggerResponse(200, "Đã xóa", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy thành viên hoặc team", typeof(ApiResponse))]
    public async Task<IActionResult> RemoveCompanyTeamMemberAsync(
        [FromRoute] Guid teamId, [FromRoute] Guid userId, CancellationToken ct)
        => (await sender.Send(new RemoveCompanyTeamMemberCommand(teamId, userId), ct))
            .ToHttpNoContent("Đã xóa nhân viên khỏi team.");
}

public sealed record UpdateTeamRequest(string Name);
public sealed record AddTeamMemberRequest(Guid UserId, bool IsLeader = false);
public sealed record DeclineTaskRequest(Guid TeamId, string Reason);
public sealed record TransferMemberRequest(Guid NewTeamId, bool IsLeader = false);
public sealed record UpdateCompanyTeamRequest(string Name);
public sealed record AddCompanyTeamMemberRequest(Guid UserId, bool IsLeader = false);
