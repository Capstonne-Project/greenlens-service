using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.BackgroundJobs;
using Hangfire;

namespace Greenlens.Infrastructure.Services;

/// <summary>
/// Enqueues notification channel dispatch on Hangfire (FCM + SMTP out of band).
/// </summary>
/// <remarks>Implements: BR-NTF-001, BR-SYS-001.</remarks>
internal sealed class NotificationDispatchScheduler(IBackgroundJobClient jobs) : INotificationDispatchScheduler
{
    public void Enqueue(Guid notificationId)
        => jobs.Enqueue<DispatchNotificationChannelsJob>(
            j => j.ExecuteAsync(notificationId, CancellationToken.None));
}
