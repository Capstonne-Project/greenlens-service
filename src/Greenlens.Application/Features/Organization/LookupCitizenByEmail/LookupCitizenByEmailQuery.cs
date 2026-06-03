using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.LookupCitizenByEmail;

/// <summary>
/// LEO searches a Citizen account by exact email before recruiting.
/// Returns basic user info for preview/confirmation on FE.
/// </summary>
public sealed record LookupCitizenByEmailQuery(
    string Email) : IRequest<Result<CitizenLookupResponse>>;

public sealed record CitizenLookupResponse(
    Guid UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    UserRole Role,
    bool IsRecruitEligible,
    string? IneligibleReason);
