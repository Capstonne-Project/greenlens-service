namespace Greenlens.Application.Common.Interfaces;

/// <summary>Resolves CompanyManager recipients for company-scoped alerts (BR-CMP-005, BR-NTF-002).</summary>
public interface ICompanyManagerRecipientQuery
{
    Task<IReadOnlyList<Guid>> GetActiveManagerIdsByCompanyAsync(Guid companyId, CancellationToken ct = default);
}
