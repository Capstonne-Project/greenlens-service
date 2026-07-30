using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.TerminateCompany;

/// <summary>
/// DEO/Admin terminates a company contract early.
/// </summary>
/// <remarks>Implements: BR-CMP-004, BR-CMP-013, BR-ADM-010.</remarks>
public sealed record TerminateCompanyCommand(Guid CompanyId, string Reason) : IRequest<Result>;
