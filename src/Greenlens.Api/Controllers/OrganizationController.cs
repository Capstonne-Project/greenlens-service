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
        Summary = "Create Department",
        Description = "Admin creates a Department of Environmental Management for a province (BR-ORG-001).")]
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
        Summary = "Create Local Office",
        Description = "Admin onboards a new ward/commune Local Environmental Office (BR-ORG-002, BR-ADM-011).")]
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
        Summary = "Assign LEO to Office",
        Description = "Admin assigns a LEO user to a Local Environmental Office (BR-ORG-002).")]
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
        Summary = "Create Team",
        Description = "Admin creates an Environmental Team (Cleanup or Inspection) under a LocalOffice (BR-ORG-003).")]
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
        Summary = "Add Team Member",
        Description = "Admin adds a user to an Environmental Team (BR-ORG-003).")]
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
