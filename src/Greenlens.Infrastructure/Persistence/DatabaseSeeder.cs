using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Persistence;

/// <summary>
/// Seeds initial data for development. Runs after migrations.
/// </summary>
internal static class DatabaseSeeder
{
    private const string AdminEmail = "admin@greenlens.com.vn";
    private const string AdminPassword = "Admin@123456";
    private const string AdminFullName = "GreenLens Admin";

    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        await SeedAdminAsync(db, logger).ConfigureAwait(false);
    }

    private static async Task SeedAdminAsync(ApplicationDbContext db, ILogger logger)
    {
        var adminExists = await db.Users
            .AnyAsync(u => u.Email == AdminEmail)
            .ConfigureAwait(false);

        if (adminExists)
            return;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword, workFactor: 12);

        var admin = User.Create(
            AdminEmail,
            passwordHash,
            AdminFullName,
            UserRole.Admin);

        // Mark email as verified since this is a seeded account
        admin.VerifyEmail();

        db.Users.Add(admin);
        await db.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation(
            "Seeded admin account: {Email} / {Password}",
            AdminEmail,
            AdminPassword);
    }
}
