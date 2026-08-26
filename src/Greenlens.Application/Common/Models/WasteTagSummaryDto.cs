namespace Greenlens.Application.Common.Models;

/// <summary>Waste tag summary for team/report responses.</summary>
public sealed record WasteTagSummaryDto(
    Guid TagId,
    string Code,
    string NameVi,
    string NameEn,
    string? IconUrl);
