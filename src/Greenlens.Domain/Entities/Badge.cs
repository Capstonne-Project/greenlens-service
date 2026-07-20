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
    /// Streak: "streak_7d", "streak_30d". Community: "hotspot_hunter", "duplicate_finder", "community_voice".
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

    public DateTime CreatedAt { get; private set; }

    public static Badge Create(
        string code, string nameVi, string nameEn,
        string? description = null, string? iconUrl = null,
        int? requiredPoints = null, int? requiredReportCount = null)
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
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
