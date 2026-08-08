using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Checklist evidence item for an InspectionReport field investigation.
/// </summary>
/// <remarks>Implements: BR-INS-033 (checklist completion).</remarks>
public sealed class InspectionEvidence : AuditableEntity
{
    private InspectionEvidence() { }

    public Guid InspectionReportId { get; private set; }
    public InspectionEvidenceCategory Category { get; private set; }
    public string? MediaUrl { get; private set; }
    public string? MimeType { get; private set; }
    public long? SizeBytes { get; private set; }
    public string? Description { get; private set; }
    public int? DurationSeconds { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }

    public InspectionReport InspectionReport { get; private set; } = default!;

    public static InspectionEvidence CreateMedia(
        Guid inspectionReportId,
        InspectionEvidenceCategory category,
        string mediaUrl,
        string mimeType,
        long sizeBytes,
        Guid uploadedByUserId,
        int? durationSeconds = null,
        string? description = null)
    {
        return new InspectionEvidence
        {
            InspectionReportId = inspectionReportId,
            Category = category,
            MediaUrl = mediaUrl,
            MimeType = mimeType,
            SizeBytes = sizeBytes,
            Description = description,
            DurationSeconds = durationSeconds,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static InspectionEvidence CreateText(
        Guid inspectionReportId,
        InspectionEvidenceCategory category,
        string description,
        Guid uploadedByUserId)
    {
        return new InspectionEvidence
        {
            InspectionReportId = inspectionReportId,
            Category = category,
            Description = description,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
