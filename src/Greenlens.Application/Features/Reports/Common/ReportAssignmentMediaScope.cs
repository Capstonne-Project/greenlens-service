using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Reports.Common;

/// <summary>
/// Limits cleanup before/after media to the active assignment cycle (BR-REP-015 reopen / reassign).
/// </summary>
internal static class ReportAssignmentMediaScope
{
    internal static IReadOnlyList<ReportMedia> FilterForAssignment(
        IEnumerable<ReportMedia> media,
        ReportAssignment? assignment,
        params MediaType[] types)
    {
        if (assignment is null)
            return [];

        var cycleStart = assignment.AssignedAt;
        var includesAfter = types.Contains(MediaType.After);

        return media
            .Where(m => types.Contains(m.Type))
            .Where(m => m.UploadedAt >= cycleStart)
            // After images are saved at resolve time (same instant as Complete()). Do not upper-bound
            // by CompletedAt — legacy rows may have UploadedAt slightly after CompletedAt.
            .Where(m => includesAfter && m.Type == MediaType.After
                || assignment.CompletedAt is null
                || m.UploadedAt <= assignment.CompletedAt)
            .OrderBy(m => m.UploadedAt)
            .ToList();
    }

    internal static string? ResolveLatestProgressNote(ReportAssignment assignment)
    {
        if (assignment.ProgressUpdates.Count > 0)
        {
            return assignment.ProgressUpdates
                .OrderByDescending(u => u.CreatedAt)
                .First()
                .ProgressNote;
        }

        return assignment.ProgressNote;
    }
}
