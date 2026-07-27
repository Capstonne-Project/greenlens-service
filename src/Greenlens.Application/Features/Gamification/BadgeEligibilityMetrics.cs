namespace Greenlens.Application.Features.Gamification;

/// <summary>Precomputed user metrics for BR-GAM-004 badge eligibility checks.</summary>
internal sealed record BadgeEligibilityMetrics(
    int VerifiedReportCount,
    int DuplicateReportCount,
    bool HasCommunityVoice,
    int MaxSubmitStreakDays);
