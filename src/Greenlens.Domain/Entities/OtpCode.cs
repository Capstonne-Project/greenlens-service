using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// OTP code for email verification or password reset.
/// </summary>
/// <remarks>OTP lifetime: 10 min, max 5 verification attempts.</remarks>
public sealed class OtpCode : BaseEntity
{
    private const int MaxAttempts = 5;

    private OtpCode() { }

    public string Email { get; private set; } = default!;
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

    public void IncrementAttempt() => AttemptCount++;

    public void MarkUsed() => IsUsed = true;
}
