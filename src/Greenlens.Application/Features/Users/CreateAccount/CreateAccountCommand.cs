using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Users.CreateAccount;

/// <summary>
/// Admin creates a new user account (Officer, CleanupTeam, Citizen).
/// Email is auto-verified on creation.
/// </summary>
public sealed record CreateAccountCommand(
    string Email,
    string Password,
    string FullName,
    UserRole Role) : IRequest<Result<CreateAccountResponse>>;
