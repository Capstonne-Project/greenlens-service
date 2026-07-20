using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.GamificationConfigs.GetGamificationConfigs;

/// <summary>
/// List all gamification point configurations.
/// </summary>
/// <remarks>Implements: BR-ADM-005.</remarks>
public sealed record GetGamificationConfigsQuery() : IRequest<Result<List<GamificationConfigItem>>>;

public sealed record GamificationConfigItem(
    Guid Id,
    string ActionType,
    int Points,
    string Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
