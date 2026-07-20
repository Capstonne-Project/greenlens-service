namespace Greenlens.Application.Features.Admin.ToggleBanUser;

public sealed record ToggleBanUserResponse(Guid UserId, bool IsBanned, string Message);
