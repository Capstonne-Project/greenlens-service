using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Invitation for a Citizen to join a LocalOffice team as Cleaner/Inspector.
/// </summary>
/// <remarks>
/// Implements: BR-ORG-021 — invitation valid 7 days, single-use.
/// BR-ORG-020 — LEO invites via email, role changes on Accept.
/// </remarks>
public sealed class StaffInvitation : AuditableEntity
{
    private StaffInvitation() { } // EF Core

    /// <summary>LEO who sent the invitation.</summary>
    public Guid InvitedByUserId { get; private set; }

    /// <summary>Citizen who is being invited.</summary>
    public Guid InvitedUserId { get; private set; }

    /// <summary>LocalOffice (ward) the citizen is being invited into.</summary>
    public Guid LocalOfficeId { get; private set; }

    /// <summary>Optional: specific team to join.</summary>
    public Guid? TeamId { get; private set; }

    /// <summary>Target role after acceptance: Cleaner or Inspector.</summary>
    public UserRole TargetRole { get; private set; }

    /// <summary>Whether the invited user should become the team's Leader on acceptance.</summary>
    public bool IsLeader { get; private set; }

    /// <summary>Current invitation status.</summary>
    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;

    /// <summary>Invitation expires after 7 days.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>When the invited user responded (accepted/declined).</summary>
    public DateTime? RespondedAt { get; private set; }

    /// <summary>Unique token for accepting via link (optional).</summary>
    public string Token { get; private set; } = string.Empty;

    // ── Navigation ──
    public User? InvitedByUser { get; private set; }
    public User? InvitedUser { get; private set; }
    public LocalOffice? LocalOffice { get; private set; }
    public EnvironmentalTeam? Team { get; private set; }

    /// <summary>Create a new pending invitation.</summary>
    public static StaffInvitation Create(
        Guid invitedByUserId,
        Guid invitedUserId,
        Guid localOfficeId,
        UserRole targetRole,
        Guid? teamId = null,
        bool isLeader = false,
        int expiryDays = 7)
    {
        return new()
        {
            InvitedByUserId = invitedByUserId,
            InvitedUserId = invitedUserId,
            LocalOfficeId = localOfficeId,
            TargetRole = targetRole,
            TeamId = teamId,
            IsLeader = isLeader,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            Token = Guid.NewGuid().ToString("N")
        };
    }

    /// <summary>Check if this invitation is still valid.</summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>Check if this invitation can still be responded to.</summary>
    public bool CanRespond => Status == InvitationStatus.Pending && !IsExpired;

    /// <summary>Citizen accepts the invitation.</summary>
    public Result Accept()
    {
        if (IsExpired)
        {
            Status = InvitationStatus.Expired;
            return Result.Failure(new Error(
                "INVITATION_EXPIRED", "Lời mời đã hết hạn.", ErrorType.BusinessRule));
        }

        if (Status != InvitationStatus.Pending)
            return Result.Failure(new Error(
                "INVITATION_ALREADY_RESPONDED", "Lời mời đã được trả lời trước đó.", ErrorType.BusinessRule));

        Status = InvitationStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>Citizen declines the invitation.</summary>
    public Result Decline()
    {
        if (IsExpired)
        {
            Status = InvitationStatus.Expired;
            return Result.Failure(new Error(
                "INVITATION_EXPIRED", "Lời mời đã hết hạn.", ErrorType.BusinessRule));
        }

        if (Status != InvitationStatus.Pending)
            return Result.Failure(new Error(
                "INVITATION_ALREADY_RESPONDED", "Lời mời đã được trả lời trước đó.", ErrorType.BusinessRule));

        Status = InvitationStatus.Declined;
        RespondedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>LEO cancels the invitation before it's been responded to.</summary>
    public Result Cancel()
    {
        if (Status != InvitationStatus.Pending)
            return Result.Failure(new Error(
                "INVITATION_ALREADY_RESPONDED", "Lời mời đã được trả lời trước đó.", ErrorType.BusinessRule));

        Status = InvitationStatus.Cancelled;
        RespondedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
