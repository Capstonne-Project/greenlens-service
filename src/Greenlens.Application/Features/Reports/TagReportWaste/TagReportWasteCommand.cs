using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.TagReportWaste;

/// <summary>
/// Officer tags a report with specific waste types.
/// Can be called any time after report is Submitted (during verify or after).
/// Replaces any existing tags with the new set.
/// </summary>
public sealed record TagReportWasteCommand(
    Guid ReportId,
    List<Guid> WasteTagIds) : IRequest<Result>;
