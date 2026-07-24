using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

/// <summary>Shared email/phone uniqueness checks for registration flows.</summary>
public static class UserRegistrationGuard
{
    /// <summary>
    /// Validates email for citizen/admin registration.
    /// Soft-deleted emails return <see cref="Errors.Auth.EmailDeletedRestoreAvailable"/> (BR-AUTH-021).
    /// </summary>
    public static async Task<Error?> ValidateNewEmailForRegistrationAsync(
        IUserRepository users,
        string email,
        CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();

        if (await users.ExistsAsync(u => u.Email == normalized, ct).ConfigureAwait(false))
            return Errors.Auth.EmailTaken;

        var deleted = await users.GetDeletedByEmailAsync(normalized, ct).ConfigureAwait(false);
        if (deleted is not null)
            return Errors.Auth.EmailDeletedRestoreAvailable;

        return null;
    }

    /// <summary>Validates email for org provisioning (no restore hint — email is taken).</summary>
    public static async Task<Error?> ValidateNewEmailForProvisioningAsync(
        IUserRepository users,
        string email,
        CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();

        if (await users.EmailExistsIncludingDeletedAsync(normalized, ct: ct).ConfigureAwait(false))
            return Errors.Organization.ManagerEmailAlreadyExists;

        return null;
    }

    public static async Task<Error?> ValidateNewPhoneAsync(
        IUserRepository users,
        string? phone,
        CancellationToken ct)
    {
        var normalized = PhoneNumberNormalizer.Normalize(phone);
        if (normalized is null)
            return null;

        if (await users.PhoneExistsIncludingDeletedAsync(normalized, ct: ct).ConfigureAwait(false))
            return Errors.Phone.PhoneAlreadyUsed;

        return null;
    }
}
