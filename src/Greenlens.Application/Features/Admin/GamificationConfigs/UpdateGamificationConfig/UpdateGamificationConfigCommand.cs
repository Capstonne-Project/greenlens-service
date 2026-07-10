using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.GamificationConfigs.UpdateGamificationConfig;

/// <summary>
/// Admin updates point value for a gamification action.
/// </summary>
/// <remarks>Implements: BR-ADM-005. Audit logged via IAuditable.</remarks>
public sealed record UpdateGamificationConfigCommand(
    Guid Id,
    int Points,
    string Description,
    bool IsActive) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "GamificationConfig";
    string? IAuditable.AuditEntityId => Id.ToString();
}
