using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.Badges.UpdateBadgeThresholds;

/// <summary>Admin updates the eligibility threshold for a badge.</summary>
/// <remarks>Implements: BR-ADM-005, BR-GAM-004.</remarks>
public sealed record UpdateBadgeThresholdsCommand(Guid Id, int Threshold) : IRequest<Result>;
