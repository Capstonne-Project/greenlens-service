using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.AddTeamMember;
using Greenlens.Application.Features.Organization.CreateTeam;
using Greenlens.Application.Features.Organization.GetTeamById;
using Greenlens.Application.Features.Organization.GetTeams;
using Greenlens.Application.Features.Organization.RemoveTeamMember;
using Greenlens.Application.Features.Organization.UpdateTeam;
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
