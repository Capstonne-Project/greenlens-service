using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Infrastructure.Identity;

/// <summary>BR-DAT-001: bcrypt ≥12 rounds.</summary>
internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
