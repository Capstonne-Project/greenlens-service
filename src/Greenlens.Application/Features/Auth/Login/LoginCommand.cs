using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<LoginResponse>>;
