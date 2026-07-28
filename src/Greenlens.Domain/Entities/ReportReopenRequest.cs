using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Citizen request to reopen a Resolved report. LEO reviews reason and evidence before approval.
/// </summary>
/// <remarks>Implements: BR-REP-015, BR-REP-022 (reject reason ≥ 20 chars).</remarks>
public sealed class ReportReopenRequest : BaseEntity
{
    private ReportReopenRequest() { }

    public Guid ReportId { get; private set; }
    public Guid RequestedBy { get; private set; }
    public string Reason { get; private set; } = default!;
    public ReopenRequestStatus Status { get; private set; } = ReopenRequestStatus.Pending;
    public Guid? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime RequestedAt { get; private set; }

    public Report Report { get; private set; } = default!;
    public User Requester { get; private set; } = default!;
    public ICollection<ReportMedia> Media { get; private set; } = [];

    public static ReportReopenRequest Create(Guid reportId, Guid requestedBy, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new ReportReopenRequest
        {
            ReportId = reportId,
            RequestedBy = requestedBy,
            Reason = reason.Trim(),
            Status = ReopenRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
    }

    public void Approve(Guid leoId)
    {
        if (Status != ReopenRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot approve reopen request from status {Status}.");

        Status = ReopenRequestStatus.Approved;
        ReviewedBy = leoId;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Reject(Guid leoId, string reason)
    {
        if (Status != ReopenRequestStatus.Pending)
            throw new InvalidOperationException($"Cannot reject reopen request from status {Status}.");

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Status = ReopenRequestStatus.Rejected;
        ReviewedBy = leoId;
        ReviewedAt = DateTime.UtcNow;
        RejectionReason = reason.Trim();
    }
}
