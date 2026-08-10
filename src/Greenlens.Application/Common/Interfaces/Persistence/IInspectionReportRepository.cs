using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IInspectionReportRepository : IGenericRepository<InspectionReport>
{
    Task<List<InspectionReport>> GetByReportIdAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>BR-INS-022: Count inspection reports for same violator identity within a period.</summary>
    Task<int> CountByViolatorInPeriodAsync(string violatorIdentity, int months, CancellationToken ct = default);

    Task<InspectionReport?> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default);

    /// <summary>Lookup payment including soft-deleted rows (unique index / idempotent delete).</summary>
    Task<PenaltyPayment?> FindPaymentByIdAsync(Guid paymentId, CancellationToken ct = default);

    /// <summary>
    /// Track a newly created payment as EF Core "Added" explicitly. Required because
    /// <see cref="PenaltyPayment"/>'s Id is generated client-side (non-default Guid) — if the
    /// entity only reaches the DbContext via navigation fixup on an <see cref="InspectionReport"/>
    /// loaded without <c>Include(ir => ir.Payments)</c>, EF Core's change detection marks it
    /// "Modified" instead of "Added", producing an UPDATE that affects 0 rows
    /// (DbUpdateConcurrencyException) instead of the intended INSERT.
    /// </summary>
    void AddPayment(PenaltyPayment payment);
}
