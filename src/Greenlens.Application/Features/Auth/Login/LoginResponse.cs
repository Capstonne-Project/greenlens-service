namespace Greenlens.Application.Features.Auth.Login;

public sealed record LoginResponse(string AccessToken, string RefreshToken, UserDto User);

public sealed record UserDto(Guid Id, string Email, string FullName, string Role, bool IsEmailVerified);
