using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.SubmitPollutionReport;

/// <summary>
/// Submit a new pollution report. Supports two image flows:
/// - AI flow:     TempImageId != null, Images == null  (ảnh đã qua POST /reports/analyze)
/// - Manual flow: TempImageId == null, Images != null  (URL từ POST /v1/media/reports/images)
/// Exactly one of the two must be provided.
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
    bool IsAnonymous,

    /// <summary>AI flow: temp_image_id từ POST /reports/analyze (TTL 15 phút).</summary>
    string? TempImageId,

    /// <summary>Manual flow: danh sách ảnh đã upload qua POST /v1/media/reports/images.</summary>
    IReadOnlyList<SubmitPollutionReportImageItem>? Images
) : IRequest<Result<SubmitPollutionReportResponse>>;
