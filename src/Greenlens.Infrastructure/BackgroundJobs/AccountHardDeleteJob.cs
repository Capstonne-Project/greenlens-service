using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.BackgroundJobs;

/// <summary>
/// Permanently deletes user accounts that have been soft-deleted for over 90 days.
/// Runs daily via Hangfire.
/// </summary>
/// <remarks>Implements: BR-AUTH-021 (hard delete after 90 days retention).</remarks>
internal sealed class AccountHardDeleteJob(
    ApplicationDbContext dbContext,
    ILogger<AccountHardDeleteJob> logger)
{
    private const int RetentionDays = 90;

    public async Task ExecuteAsync()
    {
        var threshold = DateTime.UtcNow.AddDays(-RetentionDays);

        var usersToDelete = await dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.DeletedAt != null && u.DeletedAt <= threshold)
            .ToListAsync()
            .ConfigureAwait(false);

        if (usersToDelete.Count == 0)
        {
            logger.LogInformation("AccountHardDeleteJob: no users to delete");
            return;
        }

        logger.LogWarning("AccountHardDeleteJob: permanently deleting {Count} users", usersToDelete.Count);

        foreach (var user in usersToDelete)
        {
            logger.LogInformation("Hard-deleting user {UserId} (email={Email}, deleted at {DeletedAt})",
                user.Id, user.Email, user.DeletedAt);

            dbContext.Users.Remove(user);
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("AccountHardDeleteJob: completed, {Count} users permanently removed", usersToDelete.Count);
    }
}
