using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Organization.AssignLeoToOffice;
using Greenlens.Application.Features.Organization.CreateDepartment;
using Greenlens.Application.Features.Organization.CreateLocalOffice;
using Greenlens.Application.Features.Organization.CreateTeam;
using Greenlens.Application.Features.Organization.AddTeamMember;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/organization")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class OrganizationController(ISender sender) : ControllerBase
{
    // ── Departments ──

    [HttpPost("departments")]
    [SwaggerOperation(
        Summary = "[Admin] Tạo Department",
        Description = "Tạo Sở Tài nguyên & Môi trường cấp Tỉnh/TP. Mỗi tỉnh chỉ có 1 Department.")]
    [SwaggerResponse(201, "Department created", typeof(ApiResponse<CreateDepartmentResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(403, "Forbidden — Admin only", typeof(ApiResponse))]
    [SwaggerResponse(409, "Province already has a department", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> CreateDepartmentAsync(
        [FromBody] CreateDepartmentCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    // ── Local Offices ──

    [HttpPost("offices")]
    [SwaggerOperation(
        Summary = "[Admin] Tạo Local Office",
        Description = "Onboard văn phòng môi trường cấp xã/phường. Sau khi tạo, báo cáo trong ward đó sẽ tự động route đến office này.")]
    [SwaggerResponse(201, "Office created", typeof(ApiResponse<CreateLocalOfficeResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(403, "Forbidden — Admin only", typeof(ApiResponse))]
    [SwaggerResponse(404, "Department or ward not found", typeof(ApiResponse))]
    [SwaggerResponse(409, "Ward already has an office", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> CreateLocalOfficeAsync(
        [FromBody] CreateLocalOfficeCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpPut("offices/{officeId:guid}/officer")]
    [SwaggerOperation(
        Summary = "[Admin] Gán LEO cho Office",
        Description = "Gán 1 user có role LEO làm người phụ trách văn phòng môi trường cấp xã/phường.")]
    [SwaggerResponse(204, "LEO assigned")]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(403, "Forbidden — Admin only", typeof(ApiResponse))]
    [SwaggerResponse(404, "Office or user not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "User role is not LEO", typeof(ApiResponse))]
    public async Task<IActionResult> AssignLeoToOfficeAsync(
        [FromRoute] Guid officeId,
        [FromBody] AssignLeoRequest request,
        CancellationToken ct)
        => (await sender.Send(new AssignLeoToOfficeCommand(officeId, request.UserId), ct)).ToHttpNoContent();

    // ── Teams ──

    [HttpPost("teams")]
    [SwaggerOperation(
        Summary = "[Admin] Tạo Team",
        Description = "Tạo đội Cleanup (dọn dẹp) hoặc Inspection (thanh tra) thuộc 1 LocalOffice.")]
    [SwaggerResponse(201, "Team created", typeof(ApiResponse<CreateTeamResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(403, "Forbidden — Admin only", typeof(ApiResponse))]
    [SwaggerResponse(404, "Office not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> CreateTeamAsync(
        [FromBody] CreateTeamCommand command,
        CancellationToken ct)
        => (await sender.Send(command, ct)).ToHttpCreated();

    [HttpPost("teams/{teamId:guid}/members")]
    [SwaggerOperation(
        Summary = "[Admin] Thêm thành viên vào Team",
        Description = "Thêm user vào đội môi trường. Role Cleanup chỉ vào team Cleanup, role Inspector chỉ vào team Inspection.")]
    [SwaggerResponse(201, "Member added", typeof(ApiResponse<AddTeamMemberResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(403, "Forbidden — Admin only", typeof(ApiResponse))]
    [SwaggerResponse(404, "Team or user not found", typeof(ApiResponse))]
    [SwaggerResponse(409, "User already in team", typeof(ApiResponse))]
    [SwaggerResponse(422, "Invalid role for team type", typeof(ApiResponse))]
    public async Task<IActionResult> AddTeamMemberAsync(
        [FromRoute] Guid teamId,
        [FromBody] AddTeamMemberRequest request,
        CancellationToken ct)
        => (await sender.Send(
            new AddTeamMemberCommand(teamId, request.UserId, request.IsLeader), ct)).ToHttpCreated();
}

/// <summary>Request body for assigning a LEO to an office.</summary>
public sealed record AssignLeoRequest(Guid UserId);

/// <summary>Request body for adding a member to a team.</summary>
public sealed record AddTeamMemberRequest(Guid UserId, bool IsLeader = false);
