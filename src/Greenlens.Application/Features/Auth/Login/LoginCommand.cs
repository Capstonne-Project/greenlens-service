using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.Login;

public sealed record LoginCommand(
    string? Email,
    string? Phone,
    string Password,
    string? CaptchaToken) : IRequest<Result<LoginResponse>>;
