using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.Badges.UpdateBadge;

/// <summary>Admin updates badge display content. Code and eligibility thresholds are read-only.</summary>
/// <remarks>Implements: BR-ADM-005, BR-ADM-010.</remarks>
public sealed record UpdateBadgeCommand(
    Guid Id,
    string NameVi,
    string NameEn,
    string? Description,
    string? IconUrl) : IRequest<Result>;
