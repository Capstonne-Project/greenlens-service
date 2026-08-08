using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.IntegrationTests.Fixtures;

/// <summary>No-op audit logger for integration tests — avoids side effects.</summary>
internal sealed class NoOpAuditLogger : IAuditLogger
{
    public Task LogAsync(
        string action,
        string entityType,
        string? entityId,
        string? oldValues,
        string? newValues,
        CancellationToken ct = default) => Task.CompletedTask;
}
