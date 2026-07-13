using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Inspection.SearchViolatingEntities;

public sealed record ViolatingEntityDto(
    Guid Id,
    string Name,
    ViolatorType Type,
    string? Address,
    string? TaxCode,
    string? IdentityNumber,
    string? PhoneNumber,
    int InspectionCount);
