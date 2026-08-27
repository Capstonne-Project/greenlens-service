using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.CommunityCleanup.Common;

/// <summary>
/// Anonymous-safe preview for Next.js OG landing pages (GET /v1/public/community-cleanups/{eventId}).
/// No participant PII, no auth-only fields.
/// </summary>
public sealed record CommunityCleanupPublicPreviewResponse(
    Guid Id,
    string Title,
    string? Description,
    CommunityCleanupStatus Status,
    DateTime StartsAt,
    DateTime? EndsAt,
    DateTime? JoinClosesAt,
    int MaxParticipants,
    int ParticipantCount,
    int SpotsLeft,
    string? MeetingNote,
    string CategoryName,
    string? ReportAddress,
    string? ThumbnailUrl,
    CommunityCleanupShareDto Share);
