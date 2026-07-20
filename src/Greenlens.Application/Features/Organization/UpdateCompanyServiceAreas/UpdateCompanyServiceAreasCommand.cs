using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.UpdateCompanyServiceAreas;

/// <summary>
/// DEO replaces the entire set of wards for a company's service area.
/// Existing entries not in the new list are removed; new entries are added.
/// </summary>
/// <remarks>Implements: BR-CMP-008 (service area management), BR-CMP-014 (N–N company ↔ ward).</remarks>
public sealed record UpdateCompanyServiceAreasCommand(
    Guid CompanyId,
    List<string> WardCodes) : IRequest<Result>;
