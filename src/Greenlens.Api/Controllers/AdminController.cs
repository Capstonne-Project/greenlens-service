using Greenlens.Api.Extensions;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Users;
using Greenlens.Application.Features.Users.DeleteUser;
using Greenlens.Application.Features.Users.GetAllUsers;
using Greenlens.Application.Features.Users.GetAllUsersWithPaged;
using Greenlens.Application.Features.Users.GetUserById;
using Greenlens.Application.Features.Users.UpdateUser;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Greenlens.Api.Controllers;

[ApiController]
[Route("v1/admin/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminController(ISender sender) : ControllerBase
{
    [HttpGet("all")]
    [SwaggerOperation(
        Summary = "Get All Users",
        Description = "Fetch all users without pagination. Admin only.")]
    [SwaggerResponse(200, "User list", typeof(ApiResponse<List<UserListItemDto>>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(403, "Forbidden — Admin only", typeof(ApiResponse))]
    public async Task<IActionResult> GetAllUsersAsync(CancellationToken ct)
        => (await sender.Send(new GetAllUsersQuery(), ct)).ToHttp();

    [HttpGet]
    [SwaggerOperation(
        Summary = "Get Users (Paged)",
        Description = "Fetch users with pagination, search, and filtering. Admin only.")]
    [SwaggerResponse(200, "Paged user list", typeof(ApiResponse<PagedList<UserListItemDto>>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(403, "Forbidden — Admin only", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> GetAllUsersWithPagedAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isEmailVerified = null,
        CancellationToken ct = default)
        => (await sender.Send(
            new GetAllUsersWithPagedQuery(page, pageSize, search, role, isEmailVerified), ct)).ToHttp();

    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get User By ID",
        Description = "Fetch a single user's detail by ID. Admin only.")]
    [SwaggerResponse(200, "User detail", typeof(ApiResponse<UserDetailDto>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(403, "Forbidden — Admin only", typeof(ApiResponse))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    public async Task<IActionResult> GetUserByIdAsync(
        [FromRoute] Guid id,
        CancellationToken ct)
        => (await sender.Send(new GetUserByIdQuery(id), ct)).ToHttp();

    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Update User",
        Description = "Admin updates a user's name, phone, role, or email verification status.")]
    [SwaggerResponse(200, "User updated", typeof(ApiResponse<UpdateUserResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(403, "Forbidden — Admin only", typeof(ApiResponse))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Validation error", typeof(ApiResponse))]
    public async Task<IActionResult> UpdateUserAsync(
        [FromRoute] Guid id,
        [FromBody] AdminUpdateUserRequest request,
        CancellationToken ct)
        => (await sender.Send(
            new UpdateUserCommand(id, request.FullName, request.PhoneNumber, request.Role, request.IsEmailVerified), ct)).ToHttp();

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete User (Soft Delete)",
        Description = "Soft-delete a user by setting IsDeleted. Admin only. Cannot delete yourself.")]
    [SwaggerResponse(200, "User deleted", typeof(ApiResponse<DeleteUserResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse))]
    [SwaggerResponse(403, "Forbidden — Admin only", typeof(ApiResponse))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    [SwaggerResponse(422, "Cannot delete self", typeof(ApiResponse))]
    public async Task<IActionResult> DeleteUserAsync(
        [FromRoute] Guid id,
        CancellationToken ct)
        => (await sender.Send(new DeleteUserCommand(id), ct)).ToHttp();
}

/// <summary>
/// Request body for admin user update (separate from Command because Command includes route-bound UserId).
/// </summary>
public sealed record AdminUpdateUserRequest(
    string? FullName,
    string? PhoneNumber,
    UserRole? Role,
    bool? IsEmailVerified);
