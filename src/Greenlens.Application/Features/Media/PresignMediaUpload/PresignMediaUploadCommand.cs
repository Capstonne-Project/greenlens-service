using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Media.PresignMediaUpload;

/// <summary>
/// Purpose folder mapping for direct-to-R2 uploads (BR-REP-001, BR-SYS-002).
/// </summary>
public enum MediaUploadPurpose
{
    ReportImage = 0,
    Before = 1,
    Progress = 2,
    After = 3,
    Comment = 4,
    Avatar = 5,
    /// <summary>BR-REP-015: Citizen evidence when requesting reopen.</summary>
    ReopenEvidence = 6,
    /// <summary>BR-INS-033: Inspector checklist evidence (scene photo / video / audio / other file).</summary>
    InspectionEvidence = 7
}

/// <summary>
/// Issue a short-lived presigned PUT URL so Mobile uploads directly to R2.
/// </summary>
/// <remarks>
/// Implements: BR-REP-001 (image types), BR-SYS-002 (object storage).
/// Client flow: POST /media/presign → PUT file to uploadUrl → use publicUrl with BE APIs.
/// </remarks>
public sealed record PresignMediaUploadCommand(
    string FileName,
    string ContentType,
    MediaUploadPurpose Purpose,
    Guid? ReportId = null,
    Guid? InspectionId = null,
    InspectionEvidenceCategory? EvidenceCategory = null,
    long? FileSizeBytes = null) : IRequest<Result<PresignMediaUploadResponse>>, INoTransaction;

public sealed record PresignMediaUploadResponse(
    string UploadUrl,
    string PublicUrl,
    string Key,
    string ContentType,
    IReadOnlyDictionary<string, string> RequiredHeaders,
    int ExpiresInSeconds,
    long MaxSizeBytes,
    MediaUploadPurpose Purpose);
