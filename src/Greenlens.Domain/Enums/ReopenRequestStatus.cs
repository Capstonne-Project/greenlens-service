namespace Greenlens.Domain.Enums;

/// <summary>Lifecycle of a citizen reopen request awaiting LEO review (BR-REP-015).</summary>
public enum ReopenRequestStatus
{
    Pending,
    Approved,
    Rejected
}
