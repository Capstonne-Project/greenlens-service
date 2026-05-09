using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Auth.RequestOtp;

public sealed record RequestOtpCommand(string Email, OtpPurpose Purpose) : IRequest<Result<RequestOtpResponse>>;

public sealed record RequestOtpResponse(string Message);
