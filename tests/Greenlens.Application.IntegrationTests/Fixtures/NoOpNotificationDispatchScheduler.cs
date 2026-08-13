using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.IntegrationTests.Fixtures;

internal sealed class NoOpNotificationDispatchScheduler : INotificationDispatchScheduler
{
    public void Enqueue(Guid notificationId)
    {
    }
}
