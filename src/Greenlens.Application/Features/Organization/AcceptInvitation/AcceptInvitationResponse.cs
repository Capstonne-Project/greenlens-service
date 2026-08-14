using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Organization.AcceptInvitation;

public sealed record AcceptInvitationResponse(
    Guid UserId,
    UserRole NewRole,
    Guid LocalOfficeId,
    Guid? TeamId,
    bool IsLeader);
