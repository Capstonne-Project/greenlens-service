using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Users.GetPublicUserProfile;

public sealed record GetPublicUserProfileQuery(Guid UserId)
    : IRequest<Result<PublicUserProfileDto>>;

/// <summary>
/// Hồ sơ công khai của một người dùng khác — dùng khi bấm vào tên/avatar
/// người gửi báo cáo hoặc tác giả bình luận.
/// </summary>
/// <remarks>
/// KHÔNG chứa PII: không Email, không PhoneNumber, không GoogleId (BR-DAT-002).
/// Chỉ thông tin người dùng đã chủ động công khai qua hoạt động cộng đồng.
/// </remarks>
public sealed record PublicUserProfileDto(
    Guid Id,
    string FullName,
    string? AvatarUrl,
    UserRole Role,
    /// <summary>Tổng điểm gamification. Null khi điểm bị khóa (BR-GAM-006).</summary>
    int? Points,
    /// <summary>Level suy ra từ điểm (BR-GAM-003). Null khi điểm bị khóa.</summary>
    int? Level,
    /// <summary>Hạng all-time. Null khi bị khóa hoặc chưa có điểm (BR-GAM-005).</summary>
    int? Rank,
    /// <summary>Số báo cáo công khai đã gửi — không tính báo cáo ẩn danh hoặc bị ẩn.</summary>
    int ReportCount,
    IReadOnlyList<string> Achievements,
    FeaturedBadgeDto? FeaturedBadge,
    DateTime JoinedAt);
