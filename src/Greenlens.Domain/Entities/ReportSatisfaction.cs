using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Reporter's satisfaction feedback after a report is resolved.
/// If not satisfied and reopened_count &lt; 2, report can be reopened.
/// </summary>
/// <remarks>Implements: BR-REP-015, 024.</remarks>
public sealed class ReportSatisfaction : BaseEntity
{
    private ReportSatisfaction() { }

    public Guid ReportId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsSatisfied { get; private set; }
    public int? Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // ── Navigation ──
    public Report Report { get; private set; } = default!;
    public User User { get; private set; } = default!;

    public static ReportSatisfaction Create(
        Guid reportId,
        Guid userId,
        bool isSatisfied,
        int? rating = null,
        string? comment = null)
    {
        return new ReportSatisfaction
        {
            ReportId = reportId,
            UserId = userId,
            IsSatisfied = isSatisfied,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };
    }
}
