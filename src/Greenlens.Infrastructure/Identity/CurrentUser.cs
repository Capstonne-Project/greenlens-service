using System.Security.Claims;
using Greenlens.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Greenlens.Infrastructure.Identity;

internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : Guid.Empty;

    public string Email =>
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public string Role =>
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool IsAuthenticated =>
        accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
