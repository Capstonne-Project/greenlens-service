using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Inspection.UploadInspectionEvidence;

/// <summary>
/// BR-INS-033: Upload checklist evidence (photo/video/audio/other) for an InspectionReport.
/// ScenePhoto requires ≥ 2 items before field report submission.
/// </summary>
public sealed record InspectionEvidenceFile(
    byte[] Bytes,
    string FileName,
    string ContentType,
    int? DurationSeconds = null);

public sealed record UploadInspectionEvidenceCommand(
    Guid InspectionId,
    InspectionEvidenceCategory Category,
    IReadOnlyList<InspectionEvidenceFile> Files,
    string? Description = null) : IRequest<Result<UploadInspectionEvidenceResponse>>;

public sealed record UploadInspectionEvidenceResponse(
    IReadOnlyList<string> UploadedUrls,
    int TotalCategoryCount);
