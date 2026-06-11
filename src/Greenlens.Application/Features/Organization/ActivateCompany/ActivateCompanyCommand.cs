using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.ActivateCompany;

/// <summary>
/// DEO activates a company (PendingActivation → Active) after CM sets password.
/// </summary>
/// <remarks>Implements: BR-CMP-003.</remarks>
public sealed record ActivateCompanyCommand(Guid CompanyId) : IRequest<Result>;
