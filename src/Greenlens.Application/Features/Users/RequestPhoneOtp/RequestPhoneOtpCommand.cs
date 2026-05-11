using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.RequestPhoneOtp;

/// <summary>
/// Request an OTP to be sent via SMS for phone number verification.
/// </summary>
public sealed record RequestPhoneOtpCommand(
    string PhoneNumber) : IRequest<Result<RequestPhoneOtpResponse>>;
