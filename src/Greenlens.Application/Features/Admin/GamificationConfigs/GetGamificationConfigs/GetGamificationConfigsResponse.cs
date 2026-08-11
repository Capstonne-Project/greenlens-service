using Greenlens.Application.Common.Models;

namespace Greenlens.Application.Features.Admin.GamificationConfigs.GetGamificationConfigs;

/// <summary>Response containing paged gamification configs for Admin Dashboard.</summary>
public sealed record GetGamificationConfigsResponse(
    IReadOnlyList<GamificationConfigItem> Items,
    PaginationMeta Pagination);

public sealed record GamificationConfigItem(
    Guid Id,
    string ActionType,
    int Points,
    string Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
