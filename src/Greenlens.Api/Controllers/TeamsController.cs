using Greenlens.Api.Attributes;
using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.AddCompanyTeamMember;
using Greenlens.Application.Features.Organization.AddTeamMember;
using Greenlens.Application.Features.Organization.CreateCompanyTeam;
using Greenlens.Application.Features.Organization.CreateTeam;
using Greenlens.Application.Features.Organization.SoftDeleteCompanyTeam;
using Greenlens.Application.Features.Organization.ToggleCompanyTeamStatus;
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
using Greenlens.Application.Features.Reports.CheckInCleanup;
using Greenlens.Application.Features.Reports.DeclineAssignment;
using Greenlens.Application.Features.Reports.EscalateCleanup;
using Greenlens.Application.Features.Reports.GetMyAssignments;
using Greenlens.Application.Features.Reports.GetMyProgressHistory;
using Greenlens.Application.Features.Reports.GetMyTaskDetail;
using Greenlens.Application.Features.Reports.GetMyTaskProgressStats;
using Greenlens.Application.Features.Reports.UpdateCleanupProgress;
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

    [HttpGet("my-tasks/progress-stats")]
    [Authorize(Roles = "Cleaner,CompanyStaff,Inspector,Admin")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff/Inspector] Thống kê tiến độ nhiệm vụ",
        Description = "Trả về số liệu tổng hợp cho dashboard \"Tiến độ\": phân bố theo trạng thái, " +
            "phân bố theo mức độ ô nhiễm, số lượng quá hạn SLA, và xu hướng hoàn thành 30 ngày gần nhất. " +
            "Cùng phạm vi team với `GET /teams/my-tasks` (không lọc theo status).")]
    [SwaggerResponse(200, "Thống kê tiến độ", typeof(ApiResponse<MyTaskProgressStatsResponse>))]
    public async Task<IActionResult> GetMyTaskProgressStatsAsync(CancellationToken ct)
        => (await sender.Send(new GetMyTaskProgressStatsQuery(), ct)).ToHttp();

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
    [SupportsIdempotency]
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
        Description = "Team từ chối task trong vòng 24 giờ sau khi được phân công. Yêu cầu lý do ≥ 20 ký tự. " +
            "Report giữ `InProgress`; LEO xem `GET /v1/reports/{reportId}/progress` (assignment `Declined`) rồi `PUT /v1/reports/{reportId}/reassign`.")]
    [SwaggerResponse(200, "Đã từ chối", typeof(ApiResponse))]
    [SwaggerResponse(422, "Quá 24h, lý do quá ngắn, hoặc assignment không ở trạng thái Assigned", typeof(ApiResponse))]
    public async Task<IActionResult> DeclineTaskAsync(
        [FromRoute] Guid reportId, [FromBody] DeclineTaskRequest request, CancellationToken ct)
        => (await sender.Send(new DeclineAssignmentCommand(reportId, request.TeamId, request.Reason), ct))
            .ToHttpNoContent("Đã từ chối task.");

    // ═══════════════════════════════════════════
    // ██  BR-CLN-002/003: CHECK-IN (PostGIS)
    // ═══════════════════════════════════════════

    [HttpPost("my-tasks/{reportId:guid}/check-in")]
    [SupportsIdempotency]
    [Authorize(Roles = "Cleaner,CompanyStaff")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff] Check-in hiện trường",
        Description = "Team check-in tại vị trí hiện trường ≤ 200m (PostGIS, BR-CLN-002/003). " +
            "Chuyển assignment: Assigned → InProgress. Ghi nhận GPS.")]
    [SwaggerResponse(200, "Đã check-in", typeof(ApiResponse))]
    [SwaggerResponse(422, "Quá xa hoặc status không hợp lệ", typeof(ApiResponse))]
    public async Task<IActionResult> CheckInAsync(
        [FromRoute] Guid reportId, [FromBody] CheckInCleanupRequest request, CancellationToken ct)
        => (await sender.Send(new CheckInCleanupCommand(
            reportId, request.TeamId, request.Latitude, request.Longitude, request.Note), ct))
            .ToHttpNoContent("Đã check-in hiện trường.");

    // ═══════════════════════════════════════════
    // ██  BR-CLN-004: UPDATE PROGRESS
    // ═══════════════════════════════════════════

    [HttpPut("my-tasks/{reportId:guid}/progress")]
    [Authorize(Roles = "Cleaner,CompanyStaff")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff] Cập nhật tiến độ cleanup",
        Description = "Team leader cập nhật tiến độ (BR-CLN-004). Phải update ≥ 1 lần/ngày khi InProgress.")]
    [SwaggerResponse(200, "Đã cập nhật", typeof(ApiResponse))]
    [SwaggerResponse(422, "Assignment không InProgress hoặc percent ngoài 0–100", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateProgressAsync(
        [FromRoute] Guid reportId, [FromBody] UpdateCleanupProgressRequest request, CancellationToken ct)
        => (await sender.Send(new UpdateCleanupProgressCommand(
            reportId, request.TeamId, request.Percent, request.Note), ct))
            .ToHttpNoContent("Đã cập nhật tiến độ.");

    // ═══════════════════════════════════════════
    // ██  BR-CLN-006: ESCALATE TO LEO
    // ═══════════════════════════════════════════

    [HttpPost("my-tasks/{reportId:guid}/escalate")]
    [Authorize(Roles = "Cleaner,CompanyStaff")]
    [Tags("🧹 Cleaner Dashboard")]
    [SwaggerOperation(
        Summary = "[Cleaner/CompanyStaff] Escalate task lên LEO",
        Description = "Team báo vượt khả năng xử lý (BR-CLN-006). " +
            "Lý do ≥ 20 ký tự. Nếu tất cả team đều escalate → report quay về Verified.")]
    [SwaggerResponse(200, "Đã escalate", typeof(ApiResponse))]
    [SwaggerResponse(422, "Lý do quá ngắn hoặc assignment không InProgress", typeof(ApiResponse))]
    public async Task<IActionResult> EscalateAsync(
        [FromRoute] Guid reportId, [FromBody] EscalateCleanupRequest request, CancellationToken ct)
        => (await sender.Send(new EscalateCleanupCommand(
            reportId, request.TeamId, request.Reason), ct))
            .ToHttpNoContent("Đã escalate lên LEO.");

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
        Description = "Trả về danh sách đội MT cộng đồng (CompanyId == null) trong phạm vi officer đăng nhập: LEO → office của mình, DEO → các office thuộc sở. " +
            "Kèm trạng thái hiện tại (Available/Busy). " +
            "Hỗ trợ lọc theo loại team, trạng thái và tình trạng rảnh/bận (isAvailable). " +
            "Admin có thể lọc thêm localOfficeId. " +
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
    [SwaggerResponse(403, "Không có quyền xem team này", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy", typeof(ApiResponse))]
    [SwaggerResponse(422, "Team không thuộc phạm vi quản lý", typeof(ApiResponse))]
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
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<CreateTeamResponse>))]
    [SwaggerResponse(404, "LEO chưa được gán office", typeof(ApiResponse))]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateTeamCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp("Đã tạo team thành công.");

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
    [SwaggerResponse(200, "Đã thêm", typeof(ApiResponse<AddTeamMemberResponse>))]
    [SwaggerResponse(403, "User không thuộc phường hoặc không có quyền", typeof(ApiResponse))]
    [SwaggerResponse(404, "Team hoặc user không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(409, "User đã trong team", typeof(ApiResponse))]
    [SwaggerResponse(422, "Role không khớp team hoặc team ngoài phạm vi", typeof(ApiResponse))]
    public async Task<IActionResult> AddMemberAsync(
        [FromRoute] Guid teamId, [FromBody] AddTeamMemberRequest request, CancellationToken ct)
        => (await sender.Send(new AddTeamMemberCommand(teamId, request.UserId, request.IsLeader), ct)).ToHttp("Đã thêm thành viên vào team.");

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
            new TransferTeamMemberCommand(teamId, userId, request.NewTeamId, request.IsLeader), ct)).ToHttp("Đã chuyển thành viên sang team mới.");

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
    [SwaggerResponse(200, "Đã tạo", typeof(ApiResponse<CreateCompanyTeamResponse>))]
    [SwaggerResponse(403, "Không phải CompanyManager", typeof(ApiResponse))]
    public async Task<IActionResult> CreateCompanyTeamAsync(
        [FromBody] CreateCompanyTeamCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttp("Đã tạo team công ty thành công.");

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

    [HttpPut("company-teams/{id:guid}/archive")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Đóng/Mở team công ty",
        Description = "CM thay đổi trạng thái hoạt động của team thuộc công ty mình. " +
            "Team bị vô hiệu hóa sẽ không nhận được task mới.")]
    [SwaggerResponse(200, "Đã thay đổi", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy team", typeof(ApiResponse))]
    public async Task<IActionResult> ToggleCompanyTeamAsync(
        [FromRoute] Guid id, [FromBody] ToggleCompanyTeamRequest request, CancellationToken ct)
        => (await sender.Send(new ToggleCompanyTeamStatusCommand(id, request.IsActive), ct))
            .ToHttpNoContent("Đã thay đổi trạng thái team.");

    [HttpDelete("company-teams/{id:guid}")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Xóa team công ty (Soft Delete)",
        Description = "Xóa mềm team. Dữ liệu team không bị mất nhưng không còn xuất hiện trên hệ thống.")]
    [SwaggerResponse(200, "Đã xóa", typeof(ApiResponse))]
    [SwaggerResponse(404, "Không tìm thấy team", typeof(ApiResponse))]
    [SwaggerResponse(409, "Team đã bị xóa", typeof(ApiResponse))]
    [SwaggerResponse(422, "Team còn nhiệm vụ đang xử lý", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteCompanyTeamAsync(
        [FromRoute] Guid id, CancellationToken ct)
        => (await sender.Send(new SoftDeleteCompanyTeamCommand(id), ct))
            .ToHttpNoContent("Đã xóa team.");

    // ── Company Team Members (CM) ──

    [HttpPost("company-teams/{teamId:guid}/members")]
    [Authorize(Roles = "CompanyManager,Admin")]
    [Tags("🏢 Company Dashboard")]
    [SwaggerOperation(
        Summary = "[CompanyManager] Thêm nhân viên vào team công ty",
        Description = "CM thêm CompanyStaff (thuộc cùng công ty) vào team. " +
            "User phải có role CompanyStaff và thuộc cùng công ty của CM. " +
            "Có thể gán làm leader.")]
    [SwaggerResponse(200, "Đã thêm", typeof(ApiResponse<AddCompanyTeamMemberResponse>))]
    [SwaggerResponse(404, "Team hoặc user không tồn tại", typeof(ApiResponse))]
    [SwaggerResponse(409, "User đã trong team", typeof(ApiResponse))]
    [SwaggerResponse(422, "User không thuộc công ty hoặc sai role", typeof(ApiResponse))]
    public async Task<IActionResult> AddCompanyTeamMemberAsync(
        [FromRoute] Guid teamId, [FromBody] AddCompanyTeamMemberRequest request, CancellationToken ct)
        => (await sender.Send(new AddCompanyTeamMemberCommand(teamId, request.UserId, request.IsLeader), ct)).ToHttp("Đã thêm nhân viên vào team.");

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
public sealed record ToggleCompanyTeamRequest(bool IsActive);
public sealed record AddCompanyTeamMemberRequest(Guid UserId, bool IsLeader = false);

public sealed record CheckInCleanupRequest(
    Guid TeamId,
    decimal Latitude,
    decimal Longitude,
    string? Note = null);

public sealed record UpdateCleanupProgressRequest(
    Guid TeamId,
    int Percent,
    string? Note = null);

public sealed record EscalateCleanupRequest(
    Guid TeamId,
    string Reason);
