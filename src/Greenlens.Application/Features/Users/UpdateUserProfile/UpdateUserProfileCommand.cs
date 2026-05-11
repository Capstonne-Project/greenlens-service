using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.UpdateUserProfile;

/// <summary>
/// User updates their own profile (name only). Phone changes via OTP verification.
/// Uses ICurrentUser — no userId param.
/// </summary>
public sealed record UpdateUserProfileCommand(
    string? FullName) : IRequest<Result<UpdateUserProfileResponse>>;
