using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetOfficeCompanies;

/// <summary>
/// LEO/Admin retrieves active companies whose service area covers
/// the same ward as the given local office.
/// </summary>
/// <remarks>Implements: BR-CMP-008 (service area), BR-CMP-005 (active only).</remarks>
public sealed record GetOfficeCompaniesQuery(Guid OfficeId) : IRequest<Result<GetOfficeCompaniesResponse>>;

public sealed record GetOfficeCompaniesResponse(IReadOnlyList<OfficeCompanyItem> Companies);

public sealed record OfficeCompanyItem(
    Guid Id,
    string Name,
    string ContractNumber,
    string ContractType,
    string Status,
    string? Phone,
    string? Email,
    int ServiceAreaCount,
    int StaffCount);
