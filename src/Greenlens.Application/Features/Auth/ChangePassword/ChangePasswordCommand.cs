using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword) : IRequest<Result<ChangePasswordResponse>>;

public sealed record ChangePasswordResponse(string Message);
