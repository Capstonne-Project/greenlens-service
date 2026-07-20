using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.SubmitPollutionReport;

/// <summary>
/// Submit a new pollution report.
/// Preferred flow: Images contains direct-R2 URLs and TempImageId optionally references
/// synchronous AI analysis of the first image.
/// Legacy flow: TempImageId alone references multipart bytes awaiting R2 persistence.
/// </summary>
public sealed record SubmitPollutionReportCommand(
    Guid CategoryId,
    Severity Severity,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    string? WardCode,
    string? ProvinceCode,

    /// <summary>Optional AI analysis ID from POST /reports/analyze-uploaded (TTL 15 minutes).</summary>
    string? TempImageId,

    /// <summary>Images uploaded directly to R2 via POST /v1/media/presign + PUT.</summary>
    IReadOnlyList<SubmitPollutionReportImageItem>? Images,

    /// <summary>Optional: citizen tự chọn loại rác khi submit (có thể bổ sung/thay đổi bởi DEO sau).</summary>
    IReadOnlyList<Guid>? WasteTagIds,

    /// <summary>BR-REP-012: hide reporter display name on public views.</summary>
    bool HideReporterName = false
) : IRequest<Result<SubmitPollutionReportResponse>>;
