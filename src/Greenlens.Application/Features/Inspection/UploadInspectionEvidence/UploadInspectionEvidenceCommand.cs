using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Inspection.UploadInspectionEvidence;

/// <summary>
/// BR-INS-033: Persist checklist evidence URLs after client direct-to-R2 upload.
/// ScenePhoto requires ≥ 2 items before field report submission.
/// </summary>
/// <remarks>
/// Client flow: POST /v1/media/presign (purpose=InspectionEvidence) → PUT to R2 → POST /evidence with public URLs.
/// </remarks>
public sealed record InspectionEvidenceMediaItem(
    string Url,
    string ContentType,
    long SizeBytes,
    int? DurationSeconds = null);

public sealed record UploadInspectionEvidenceCommand(
    Guid InspectionId,
    InspectionEvidenceCategory Category,
    IReadOnlyList<InspectionEvidenceMediaItem> Items,
    string? Description = null) : IRequest<Result<UploadInspectionEvidenceResponse>>;

public sealed record UploadInspectionEvidenceResponse(
    IReadOnlyList<string> UploadedUrls,
    int TotalCategoryCount);
