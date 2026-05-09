using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Auth.VerifyOtp;

public sealed record VerifyOtpCommand(string Email, string OtpCode, OtpPurpose Purpose) : IRequest<Result<VerifyOtpResponse>>;

public sealed record VerifyOtpResponse(string Message, bool IsVerified);
