using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Stores historical password hashes for a user.
/// </summary>
/// <remarks>Implements: BR-AUTH-020 — new password must not match last 3 passwords.</remarks>
public sealed class PasswordHistory : BaseEntity
{
    private PasswordHistory() { } // EF Core constructor

    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public User? User { get; private set; }

    public static PasswordHistory Create(Guid userId, string passwordHash)
        => new()
        {
            UserId = userId,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };
}
