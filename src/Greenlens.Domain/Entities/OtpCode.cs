using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// OTP code for email verification, password reset, or phone verification.
/// </summary>
/// <remarks>OTP lifetime: 10 min (email), 5 min (phone). Max 5 verification attempts.</remarks>
public sealed class OtpCode : BaseEntity
{
    private const int MaxAttempts = 5;

    private OtpCode() { }

    public string Email { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public string CodeHash { get; private set; } = default!;
    public OtpPurpose Purpose { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsUsed { get; private set; }
    public int AttemptCount { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool HasExceededMaxAttempts => AttemptCount >= MaxAttempts;
    public bool IsValid => !IsUsed && !IsExpired && !HasExceededMaxAttempts;

    public static OtpCode Create(string email, string codeHash, OtpPurpose purpose, int lifetimeMinutes = 10)
    {
        return new OtpCode
        {
            Email = email.ToLowerInvariant(),
            CodeHash = codeHash,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(lifetimeMinutes),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            AttemptCount = 0
        };
    }

    /// <summary>Create OTP for phone number verification (5 min lifetime).</summary>
    public static OtpCode CreateForPhone(string phoneNumber, string codeHash, int lifetimeMinutes = 5)
    {
        return new OtpCode
        {
            Email = string.Empty,
            PhoneNumber = phoneNumber,
            CodeHash = codeHash,
            Purpose = OtpPurpose.PhoneVerification,
            ExpiresAt = DateTime.UtcNow.AddMinutes(lifetimeMinutes),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false,
            AttemptCount = 0
        };
    }

    public void IncrementAttempt() => AttemptCount++;

    public void MarkUsed() => IsUsed = true;
}
