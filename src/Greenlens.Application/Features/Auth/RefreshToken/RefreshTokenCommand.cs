using Greenlens.Application.Features.Auth.Login;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<LoginResponse>>;
