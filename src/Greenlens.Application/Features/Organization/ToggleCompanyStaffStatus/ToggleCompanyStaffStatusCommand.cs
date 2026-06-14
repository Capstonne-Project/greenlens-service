using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.ToggleCompanyStaffStatus;

/// <summary>
/// CompanyManager toggles a staff member's active status (deactivate/reactivate).
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record ToggleCompanyStaffStatusCommand(
    Guid UserId,
    bool IsActive) : IRequest<Result>;
