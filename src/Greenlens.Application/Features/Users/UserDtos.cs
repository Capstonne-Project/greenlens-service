using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Users;

/// <summary>
/// Lightweight DTO for user listing (admin views).
/// </summary>
public sealed record UserListItemDto(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    UserRole Role,
    bool IsEmailVerified,
    DateTime CreatedAt);

/// <summary>
/// Detailed DTO for single user view (admin detail).
/// </summary>
public sealed record UserDetailDto(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    UserRole Role,
    bool IsEmailVerified,
    string? GoogleId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

/// <summary>
/// Authenticated user's own profile, including gamification summary.
/// </summary>
public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    UserRole Role,
    bool IsEmailVerified,
    string? GoogleId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<string> Achievements,
    int TotalPoints,
    int Level,
    bool IsGamificationLocked,
    int? Rank,
    FeaturedBadgeDto? FeaturedBadge);

/// <summary>Huy hiệu người dùng chọn hiển thị nổi bật trên hồ sơ (BR-GAM-004).</summary>
public sealed record FeaturedBadgeDto(
    Guid BadgeId, string NameVi, string NameEn, string? IconUrl);
