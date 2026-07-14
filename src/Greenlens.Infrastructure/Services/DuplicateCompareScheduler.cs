using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.BackgroundJobs;
using Hangfire;

namespace Greenlens.Infrastructure.Services;

/// <summary>
/// Enqueues the Tier 2 AI image-compare on Hangfire so report submission stays fast.
/// </summary>
/// <remarks>Implements: BR-REP-030, BR-AI-002 (Tier 2 out of band), BR-SYS-001.</remarks>
internal sealed class DuplicateCompareScheduler(IBackgroundJobClient jobs) : IDuplicateCompareScheduler
{
    public void Enqueue(Guid reportId, Guid candidateReportId)
        => jobs.Enqueue<CompareDuplicateImagesJob>(
            j => j.ExecuteAsync(reportId, candidateReportId, CancellationToken.None));
}
