using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// A gamification badge that can be earned by users (BR-GAM-004).
/// Badges are seed data managed by Admin (BR-ADM-005).
/// </summary>
public sealed class Badge : BaseEntity
{
    private Badge() { } // EF Core

    /// <summary>
    /// Unique code. Milestone: "first_report", "eco_warrior", "green_champion", "earth_guardian".
    /// Streak: "streak_7d", "streak_30d". Community: "duplicate_finder", "community_voice", "cleanup_hero".
    /// Level: "rising_star", "eco_expert", "green_legend".
    /// </summary>
    public string Code { get; private set; } = default!;
    public string NameVi { get; private set; } = default!;
    public string NameEn { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>If set, badge is auto-awarded when user reaches this point threshold.</summary>
    public int? RequiredPoints { get; private set; }

    /// <summary>If set, badge is auto-awarded when user reaches this report count.</summary>
    public int? RequiredReportCount { get; private set; }

    /// <summary>If set (streak badges), badge is auto-awarded when user reaches this many consecutive submit days.</summary>
    public int? RequiredStreakDays { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static Badge Create(
        string code, string nameVi, string nameEn,
        string? description = null, string? iconUrl = null,
        int? requiredPoints = null, int? requiredReportCount = null,
        int? requiredStreakDays = null)
    {
        return new Badge
        {
            Code = code,
            NameVi = nameVi,
            NameEn = nameEn,
            Description = description,
            IconUrl = iconUrl,
            RequiredPoints = requiredPoints,
            RequiredReportCount = requiredReportCount,
            RequiredStreakDays = requiredStreakDays,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>BR-ADM-005: Admin updates display content (not eligibility thresholds).</summary>
    public void Update(string nameVi, string nameEn, string? description, string? iconUrl)
    {
        NameVi = nameVi;
        NameEn = nameEn;
        Description = description;
        IconUrl = iconUrl;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
