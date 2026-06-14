using Greenlens.Application.Features.Organization.GetCompanyById;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetMyCompany;

/// <summary>
/// CompanyManager views their own company profile (1 CM = 1 Company).
/// Reuses CompanyDetailResponse from GetCompanyById.
/// </summary>
/// <remarks>Implements: BR-CMP-001.</remarks>
public sealed record GetMyCompanyQuery() : IRequest<Result<CompanyDetailResponse>>;
