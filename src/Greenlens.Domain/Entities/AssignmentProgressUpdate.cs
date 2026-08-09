using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Immutable snapshot of one progress update by a cleanup team leader.
/// </summary>
/// <remarks>Implements: BR-CLN-004 (progress tracking), BR-OFF-011 (multi-team).</remarks>
public sealed class AssignmentProgressUpdate : BaseEntity
{
    private AssignmentProgressUpdate() { }

    public Guid AssignmentId { get; private set; }
    public Guid ReportId { get; private set; }
    public int ProgressPercent { get; private set; }
    public string? ProgressNote { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ReportAssignment Assignment { get; private set; } = default!;
    public User? UpdatedByUser { get; private set; }
    public ICollection<ReportMedia> Media { get; private set; } = [];

    public static AssignmentProgressUpdate Create(
        Guid assignmentId,
        Guid reportId,
        int progressPercent,
        string? progressNote,
        Guid updatedByUserId)
    {
        if (progressPercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(progressPercent), "Percent must be 0–100.");

        return new AssignmentProgressUpdate
        {
            AssignmentId = assignmentId,
            ReportId = reportId,
            ProgressPercent = progressPercent,
            ProgressNote = progressNote,
            UpdatedByUserId = updatedByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
