using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName) : IRequest<Result<RegisterResponse>>;
