using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Offline draft saved by the user before submitting a report.
/// Max 3 per user, auto-delete after 7 days idle.
/// </summary>
/// <remarks>Implements: BR-REP-019.</remarks>
public sealed class ReportDraft : BaseEntity
{
    private ReportDraft() { }

    public Guid UserId { get; private set; }
    public string Payload { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // ── Navigation ──
    public User User { get; private set; } = default!;

    public static ReportDraft Create(Guid userId, string payload)
    {
        var now = DateTime.UtcNow;
        return new ReportDraft
        {
            UserId = userId,
            Payload = payload,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdatePayload(string payload)
    {
        Payload = payload;
        UpdatedAt = DateTime.UtcNow;
    }
}
