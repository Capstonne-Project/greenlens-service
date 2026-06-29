using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.ReleaseStaff;

/// <summary>Reverts a staff member back to Citizen role, removes from office + teams.</summary>
public sealed record ReleaseStaffCommand(Guid UserId) : IRequest<Result>;
