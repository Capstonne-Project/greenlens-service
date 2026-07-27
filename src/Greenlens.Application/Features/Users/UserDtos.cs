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
    int? Rank);
