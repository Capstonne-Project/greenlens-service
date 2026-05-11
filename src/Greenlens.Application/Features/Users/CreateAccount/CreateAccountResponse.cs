using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Users.CreateAccount;

public sealed record CreateAccountResponse(
    Guid UserId,
    string Email,
    string FullName,
    UserRole Role,
    string Message);
