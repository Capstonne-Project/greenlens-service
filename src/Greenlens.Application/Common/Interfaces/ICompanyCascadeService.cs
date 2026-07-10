namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// BR-CMP-013: Cascading deactivation service.
/// When a company is Suspended/Terminated/Expired, auto-declines active assignments
/// and reverts affected reports to Verified so LEO can reassign.
/// </summary>
public interface ICompanyCascadeService
{
    /// <summary>
    /// Decline all active assignments for the company's teams,
    /// revert orphaned reports to Verified, and notify LEO.
    /// </summary>
    Task CascadeDeactivationAsync(Guid companyId, string reason, CancellationToken ct);
}
