using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.VerifyPhoneOtp;

/// <summary>
/// Verify phone OTP and update user's phone number.
/// </summary>
public sealed record VerifyPhoneOtpCommand(
    string PhoneNumber,
    string OtpCode) : IRequest<Result<VerifyPhoneOtpResponse>>;
