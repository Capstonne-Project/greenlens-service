using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface INotificationRepository : IGenericRepository<Notification>
{
    /// <summary>BR-NTF-001: Mark all unread notifications for one recipient (single SQL batch).</summary>
    Task<int> MarkAllAsReadForRecipientAsync(Guid recipientId, CancellationToken ct = default);
}
