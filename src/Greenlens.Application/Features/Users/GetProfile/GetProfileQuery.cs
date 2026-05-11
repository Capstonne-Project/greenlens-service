using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.GetProfile;

/// <summary>
/// Get current authenticated user's profile. No params — uses ICurrentUser from token.
/// </summary>
public sealed record GetProfileQuery() : IRequest<Result<UserDetailDto>>;
