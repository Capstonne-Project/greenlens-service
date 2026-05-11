using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// User-submitted flag on a report (duplicate, invalid, spam, inappropriate).
/// Unique constraint: (report_id, flagger_id, flag_type) — one flag per type per user.
/// </summary>
/// <remarks>Implements: BR-REP-033. When flagCount ≥ 3, notify Officer.</remarks>
public sealed class ReportFlag : BaseEntity
{
    private ReportFlag() { }

    public Guid ReportId { get; private set; }
    public Guid FlaggerId { get; private set; }
    public FlagType FlagType { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // ── Navigation ──
    public Report Report { get; private set; } = default!;
    public User Flagger { get; private set; } = default!;

    public static ReportFlag Create(
        Guid reportId,
        Guid flaggerId,
        FlagType flagType,
        string? reason = null)
    {
        return new ReportFlag
        {
            ReportId = reportId,
            FlaggerId = flaggerId,
            FlagType = flagType,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };
    }
}
