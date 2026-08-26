namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Generates a short, professional Vietnamese description for a pollution report
/// from the AI classification result (category + severity + trash subtypes).
/// Text-only — does not re-send the image.
/// </summary>
public interface IReportDescriptionGenerator
{
    /// <summary>
    /// Returns a 1–2 sentence Vietnamese description, or null when the LLM
    /// is unavailable/unconfigured/times out (best-effort, never blocks analyze).
    /// </summary>
    Task<string?> GenerateAsync(ReportDescriptionContext context, CancellationToken ct = default);
}

public sealed record ReportDescriptionContext(
    string CategoryNameVi,
    string Severity,
    IReadOnlyList<string> TrashSubtypeLabels,
    double PollutionCoverageRatio,
    IReadOnlyList<ReportDescriptionSubtype> Subtypes);

/// <summary>One detected trash subtype with its count, for a richer generated description.</summary>
public sealed record ReportDescriptionSubtype(string Label, int Count);
