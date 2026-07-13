using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.UploadInspectionEvidence;

/// <summary>
/// BR-INS-010: Upload field investigation evidence photos for an InspectionReport.
/// At least 2 photos are required before issuing penalty.
/// Images are stored as ReportMedia (MediaType.Inspection) linked to the parent Report.
/// </summary>
public sealed record InspectionEvidenceFile(byte[] Bytes, string FileName, string ContentType);

public sealed record UploadInspectionEvidenceCommand(
    Guid InspectionId,
    IReadOnlyList<InspectionEvidenceFile> Images) : IRequest<Result<UploadInspectionEvidenceResponse>>;

public sealed record UploadInspectionEvidenceResponse(IReadOnlyList<string> UploadedImageUrls, int TotalEvidenceCount);
