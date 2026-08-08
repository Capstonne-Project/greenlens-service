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

public sealed record CommunityCleanupMediaDto(
    List<string> BeforeImageUrls,
    List<string> ProgressImageUrls,
    List<string> AfterImageUrls);

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
    string? ReportDescription,
    List<string> ReportImageUrls,
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
    CommunityCleanupMediaSummaryDto MediaSummary,
    CommunityCleanupMediaDto Media);

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

/// <summary>Per-status counts of the office's community-cleanup queue.</summary>
public sealed record CommunityCleanupStatusCountDto(
    CommunityCleanupStatus Status,
    int Count);

/// <summary>Aggregate counters — GET /v1/community-cleanups/office-queue/stats.</summary>
public sealed record CommunityCleanupQueueStatsResponse(
    List<CommunityCleanupStatusCountDto> CountsByStatus,
    int TotalParticipants,
    int TotalMediaCount);
