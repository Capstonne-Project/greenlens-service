using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetCompanyServiceAreas;

/// <summary>
/// DEO/Admin retrieves the list of wards a company is responsible for.
/// </summary>
/// <remarks>Implements: BR-CMP-008 (service area query).</remarks>
public sealed record GetCompanyServiceAreasQuery(Guid CompanyId) : IRequest<Result<GetCompanyServiceAreasResponse>>;

public sealed record GetCompanyServiceAreasResponse(
    Guid CompanyId,
    string CompanyName,
    IReadOnlyList<ServiceAreaItem> ServiceAreas);

public sealed record ServiceAreaItem(
    Guid Id,
    string WardCode,
    string WardName,
    string ProvinceCode,
    string ProvinceName,
    DateTime CreatedAt);
