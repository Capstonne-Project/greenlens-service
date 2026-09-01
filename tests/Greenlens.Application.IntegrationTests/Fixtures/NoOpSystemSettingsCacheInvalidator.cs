using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Application.IntegrationTests.Fixtures;

internal sealed class NoOpSystemSettingsCacheInvalidator : ISystemSettingsCacheInvalidator
{
    public Task InvalidateAsync(CancellationToken ct = default) => Task.CompletedTask;
}
