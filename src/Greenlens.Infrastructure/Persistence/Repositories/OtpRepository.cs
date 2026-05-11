using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class OtpRepository(ApplicationDbContext context)
    : GenericRepository<OtpCode>(context), IOtpRepository
{
    public async Task<OtpCode?> GetLatestValidAsync(
        string email, OtpPurpose purpose, CancellationToken ct = default) =>
        await DbSet
            .Where(o => o.Email == email.ToLowerInvariant()
                     && o.Purpose == purpose
                     && !o.IsUsed
                     && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    public async Task InvalidateAllAsync(
        string email, OtpPurpose purpose, CancellationToken ct = default)
    {
        var otps = await DbSet
            .Where(o => o.Email == email.ToLowerInvariant()
                     && o.Purpose == purpose
                     && !o.IsUsed)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var otp in otps)
            otp.MarkUsed();
    }

    // ── Phone-based OTP methods ──

    public async Task<OtpCode?> GetLatestValidByPhoneAsync(
        string phoneNumber, CancellationToken ct = default) =>
        await DbSet
            .Where(o => o.PhoneNumber == phoneNumber
                     && o.Purpose == OtpPurpose.PhoneVerification
                     && !o.IsUsed
                     && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    public async Task InvalidateAllByPhoneAsync(
        string phoneNumber, CancellationToken ct = default)
    {
        var otps = await DbSet
            .Where(o => o.PhoneNumber == phoneNumber
                     && o.Purpose == OtpPurpose.PhoneVerification
                     && !o.IsUsed)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var otp in otps)
            otp.MarkUsed();
    }

    public async Task<int> CountTodayByPhoneAsync(
        string phoneNumber, CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        return await DbSet
            .CountAsync(o => o.PhoneNumber == phoneNumber
                          && o.Purpose == OtpPurpose.PhoneVerification
                          && o.CreatedAt >= todayUtc, ct)
            .ConfigureAwait(false);
    }
}
