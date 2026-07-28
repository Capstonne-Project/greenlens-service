namespace Greenlens.Application.Common.Interfaces;

/// <summary>Resolves LEO/DEO notification recipients for report-scoped alerts (BR-NTF-002).</summary>
public interface IOfficerRecipientQuery
{
    Task<IReadOnlyList<Guid>> GetLeoIdsByOfficeAsync(Guid officeId, CancellationToken ct = default);

    /// <summary>Primary officer: LEO by office, else first DEO by department.</summary>
    Task<Guid?> GetPrimaryOfficerIdAsync(
        Guid? assignedOfficeId,
        Guid? assignedDepartmentId,
        CancellationToken ct = default);
}
