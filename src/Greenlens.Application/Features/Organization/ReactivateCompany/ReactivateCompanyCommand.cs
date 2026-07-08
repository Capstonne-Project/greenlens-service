using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.ReactivateCompany;

/// <summary>
/// DEO/Admin reactivates a suspended company.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record ReactivateCompanyCommand(Guid CompanyId) : IRequest<Result>;
