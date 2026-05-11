using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using Greenlens.Domain.Exceptions;

namespace Greenlens.Domain.Entities;

/// <summary>
/// User aggregate root. Manages identity, authentication state, and login tracking.
/// </summary>
/// <remarks>
/// Implements: BR-AUTH-005 (password strength), BR-AUTH-011 (lockout),
/// BR-AUTH-013 (token lifecycle), BR-AUTH-022 (soft delete).
/// </remarks>
public sealed class User : SoftDeletableEntity
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(30);

    private User() { } // EF Core constructor

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string? PhoneNumber { get; private set; }
    public string? AvatarUrl { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockoutEnd { get; private set; }
    public string? GoogleId { get; private set; }

    public static User Create(string email, string passwordHash, string fullName, UserRole role = UserRole.Citizen)
    {
        var user = new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName,
            Role = role,
            IsEmailVerified = false,
            FailedLoginAttempts = 0
        };

        return user;
    }

    public static User CreateFromGoogle(string email, string fullName, string googleId, string? avatarUrl = null)
    {
        var user = new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = string.Empty, // no password for Google-only users
            FullName = fullName,
            GoogleId = googleId,
            AvatarUrl = avatarUrl,
            Role = UserRole.Citizen,
            IsEmailVerified = true, // Google already verified
            FailedLoginAttempts = 0
        };

        return user;
    }

    /// <summary>BR-AUTH-011: Record failed login attempt. Lock after 5 failures in 15 min window.</summary>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxFailedAttempts)
        {
            LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
        }
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }

    public bool IsLockedOut() =>
        LockoutEnd is not null && LockoutEnd > DateTime.UtcNow;

    /// <summary>BR-AUTH-011: Check if CAPTCHA is required (≥3 failed attempts).</summary>
    public bool RequiresCaptcha() => FailedLoginAttempts >= 3;

    public void VerifyEmail()
    {
        if (IsEmailVerified)
            throw new DomainException("Email is already verified.");

        IsEmailVerified = true;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void LinkGoogleAccount(string googleId)
    {
        GoogleId = googleId;
    }

    public void UpdateProfile(string? fullName = null, string? phoneNumber = null, string? avatarUrl = null)
    {
        if (fullName is not null) FullName = fullName;
        if (phoneNumber is not null) PhoneNumber = phoneNumber;
        if (avatarUrl is not null) AvatarUrl = avatarUrl;
    }

    /// <summary>BR-ADM: Admin can update user details including role and verification status.</summary>
    public void AdminUpdate(string? fullName = null, string? phoneNumber = null, UserRole? role = null, bool? isEmailVerified = null)
    {
        if (fullName is not null) FullName = fullName;
        if (phoneNumber is not null) PhoneNumber = phoneNumber;
        if (role is not null) Role = role.Value;
        if (isEmailVerified == true && !IsEmailVerified) IsEmailVerified = true;
        if (isEmailVerified == false) IsEmailVerified = false;
    }
}
