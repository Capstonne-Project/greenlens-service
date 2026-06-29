using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ToggleBanUser;

/// <summary>BR-AUTH-015: Toggle ban status for a user.</summary>
public sealed record ToggleBanUserCommand(Guid UserId) : IRequest<Result<ToggleBanUserResponse>>;
