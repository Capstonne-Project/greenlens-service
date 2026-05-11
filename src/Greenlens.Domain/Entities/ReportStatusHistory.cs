using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Immutable audit trail for every status change on a report.
/// </summary>
public sealed class ReportStatusHistory : BaseEntity
{
    private ReportStatusHistory() { }

    public Guid ReportId { get; private set; }
    public ReportStatus? FromStatus { get; private set; }
    public ReportStatus ToStatus { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public string? Reason { get; private set; }
    public string? Metadata { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // ── Navigation ──
    public Report Report { get; private set; } = default!;
    public User? ChangedByUser { get; private set; }

    public static ReportStatusHistory Create(
        Guid reportId,
        ReportStatus? fromStatus,
        ReportStatus toStatus,
        Guid? changedBy = null,
        string? reason = null,
        string? metadata = null)
    {
        return new ReportStatusHistory
        {
            ReportId = reportId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            ChangedBy = changedBy,
            Reason = reason,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        };
    }
}
