using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Organization.RecruitStaff;

/// <summary>
/// LEO recruits a Citizen into their LocalOffice as Cleaner/Inspector,
/// optionally adding them to a team in the same transaction.
/// </summary>
/// <remarks>Implements: BR-ORG-005 (staff recruitment).</remarks>
public sealed record RecruitStaffCommand(
    string Email,
    UserRole TargetRole,
    Guid? TeamId = null,
    bool? IsLeader = null) : IRequest<Result<RecruitStaffResponse>>;

public sealed record RecruitStaffResponse(
    Guid UserId,
    string Email,
    string FullName,
    UserRole AssignedRole,
    Guid LocalOfficeId,
    Guid? TeamId,
    Guid? TeamMemberId,
    bool IsLeader);
