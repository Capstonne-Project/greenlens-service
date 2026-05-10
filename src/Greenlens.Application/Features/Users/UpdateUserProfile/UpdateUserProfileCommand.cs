using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.UpdateUserProfile;

/// <summary>
/// User updates their own profile (name, phone). Uses ICurrentUser — no userId param.
/// </summary>
public sealed record UpdateUserProfileCommand(
    string? FullName,
    string? PhoneNumber) : IRequest<Result<UpdateUserProfileResponse>>;
