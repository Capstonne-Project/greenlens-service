using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IOtpRepository : IGenericRepository<OtpCode>
{
    Task<OtpCode?> GetLatestValidAsync(string email, OtpPurpose purpose, CancellationToken ct = default);
    Task InvalidateAllAsync(string email, OtpPurpose purpose, CancellationToken ct = default);
}
