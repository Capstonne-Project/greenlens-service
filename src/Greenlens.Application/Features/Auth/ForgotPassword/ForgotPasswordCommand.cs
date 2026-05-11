using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<ForgotPasswordResponse>>;

public sealed record ForgotPasswordResponse(string Message);
