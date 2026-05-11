using Greenlens.Application.Features.Users.VerifyPhoneFirebase;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.VerifyPhoneFirebase;

/// <summary>
/// Verify a phone number using a Firebase Phone Auth ID token.
/// FE handles OTP sending/entry via Firebase SDK; BE just verifies the resulting token.
/// </summary>
public sealed record VerifyPhoneFirebaseCommand(
    string FirebaseIdToken) : IRequest<Result<VerifyPhoneFirebaseResponse>>;
