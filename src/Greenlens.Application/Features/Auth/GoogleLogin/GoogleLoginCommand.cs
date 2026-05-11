using Greenlens.Application.Features.Auth.Login;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.GoogleLogin;

public sealed record GoogleLoginCommand(string IdToken) : IRequest<Result<LoginResponse>>;
