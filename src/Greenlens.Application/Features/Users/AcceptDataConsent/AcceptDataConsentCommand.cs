using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.AcceptDataConsent;

/// <summary>
/// User explicitly accepts data processing consent (photos, GPS location).
/// Must be called before first report submission.
/// </summary>
/// <remarks>Implements: BR-DAT-005 (consent before sending photos/GPS).</remarks>
public sealed record AcceptDataConsentCommand : IRequest<Result>;
