using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Organization.GetMyInvitations;

public sealed record InvitationDto(
    Guid InvitationId,
    Guid InvitedByUserId,
    string InvitedByName,
    UserRole TargetRole,
    string OfficeName,
    string? TeamName,
    InvitationStatus Status,
    DateTime ExpiresAt,
    DateTime CreatedAt);
