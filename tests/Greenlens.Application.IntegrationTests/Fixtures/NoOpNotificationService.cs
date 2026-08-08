using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.IntegrationTests.Fixtures;

/// <summary>No-op notification service for integration tests — avoids sending real notifications.</summary>
internal sealed class NoOpNotificationService : INotificationService
{
    public Task SendRawAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        Guid? referenceId = null,
        CancellationToken ct = default) => Task.CompletedTask;

    public Task SendFromTemplateAsync(
        Guid recipientId,
        NotificationType type,
        Dictionary<string, string> placeholders,
        Guid? referenceId = null,
        CancellationToken ct = default) => Task.CompletedTask;
}
