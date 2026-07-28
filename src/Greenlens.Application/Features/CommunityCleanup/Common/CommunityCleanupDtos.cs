using Greenlens.Application.Common.Models;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.CommunityCleanup.Common;

public sealed record CommunityCleanupLeaderDto(
    Guid UserId,
    string FullName,
    Guid TeamId,
    string TeamName);

public sealed record CommunityCleanupMediaSummaryDto(
    int BeforeCount,
    int ProgressCount,
    int AfterCount);

public sealed record CommunityCleanupMyParticipationDto(
    CommunityCleanupParticipantStatus Status,
    DateTime JoinedAt,
    CommunityCleanupParticipantRole Role);

/// <summary>Full event detail — docs/community-cleanup-feature-spec.md §7.4.</summary>
public sealed record CommunityCleanupEventDetailResponse(
    Guid Id,
    Guid ReportId,
    string ReportCode,
    CommunityCleanupStatus Status,
    string Title,
    string? Description,
    CommunityCleanupLeaderDto Leader,
    DateTime JoinOpensAt,
    DateTime? JoinClosesAt,
    DateTime StartsAt,
    DateTime? EndsAt,
    int MaxParticipants,
    int ParticipantCount,
    int SpotsLeft,
    int ProgressPercent,
    string? ProgressNote,
    string? MeetingNote,
    decimal? MeetingLatitude,
    decimal? MeetingLongitude,
    decimal ReportLatitude,
    decimal ReportLongitude,
    string? ReportAddress,
    string CategoryName,
    Severity Severity,
    string? ThumbnailUrl,
    CommunityCleanupMyParticipationDto? MyParticipation,
    bool IsLeader,
    CommunityCleanupMediaSummaryDto MediaSummary);

/// <summary>Compact list item for open/my/led-by-me lists.</summary>
public sealed record CommunityCleanupListItemDto(
    Guid Id,
    Guid ReportId,
    string ReportCode,
    CommunityCleanupStatus Status,
    string Title,
    Guid LeaderUserId,
    string LeaderFullName,
    DateTime StartsAt,
    DateTime? JoinClosesAt,
    int MaxParticipants,
    int ParticipantCount,
    int SpotsLeft,
    int ProgressPercent,
    decimal ReportLatitude,
    decimal ReportLongitude,
    string? ThumbnailUrl,
    CommunityCleanupMyParticipationDto? MyParticipation);

public sealed record CommunityCleanupParticipantDto(
    Guid UserId,
    string FullName,
    string? AvatarUrl,
    CommunityCleanupParticipantRole Role,
    CommunityCleanupParticipantStatus Status,
    DateTime JoinedAt,
    DateTime? CheckedInAt);

public sealed record CommunityCleanupListResponse(
    List<CommunityCleanupListItemDto> Items,
    PaginationMeta Pagination);

public sealed record CommunityCleanupParticipantsResponse(
    List<CommunityCleanupParticipantDto> Items,
    PaginationMeta Pagination);
