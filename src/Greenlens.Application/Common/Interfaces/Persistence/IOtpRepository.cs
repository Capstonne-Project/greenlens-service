using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IOtpRepository : IGenericRepository<OtpCode>
{
    Task<OtpCode?> GetLatestValidAsync(string email, OtpPurpose purpose, CancellationToken ct = default);
    Task InvalidateAllAsync(string email, OtpPurpose purpose, CancellationToken ct = default);

    // Phone-based OTP methods
    Task<OtpCode?> GetLatestValidByPhoneAsync(string phoneNumber, CancellationToken ct = default);
    Task InvalidateAllByPhoneAsync(string phoneNumber, CancellationToken ct = default);
    Task<int> CountTodayByPhoneAsync(string phoneNumber, CancellationToken ct = default);
}
