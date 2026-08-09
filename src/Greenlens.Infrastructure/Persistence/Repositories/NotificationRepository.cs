using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(ApplicationDbContext context)
    : GenericRepository<Notification>(context), INotificationRepository
{
    public async Task<int> MarkAllAsReadForRecipientAsync(Guid recipientId, CancellationToken ct = default)
    {
        var readAt = DateTime.UtcNow;

        return await Context.Notifications
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, readAt), ct)
            .ConfigureAwait(false);
    }
}
