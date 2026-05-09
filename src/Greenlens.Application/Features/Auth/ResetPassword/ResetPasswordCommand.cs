using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string OtpCode,
    string NewPassword) : IRequest<Result<ResetPasswordResponse>>;

public sealed record ResetPasswordResponse(string Message);
