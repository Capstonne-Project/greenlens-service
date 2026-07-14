namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Schedules the Tier 2 AI image-compare as a background job so report submission stays fast (BR-SYS-001).
/// Implemented in Infrastructure over Hangfire — keeps the Application layer free of the job runner.
/// </summary>
/// <remarks>Implements: BR-REP-030, BR-AI-002 (Tier 2 runs out of band).</remarks>
public interface IDuplicateCompareScheduler
{
    /// <summary>Enqueue an AI image comparison between a freshly-flagged report and its Tier 1 candidate.</summary>
    void Enqueue(Guid reportId, Guid candidateReportId);
}
