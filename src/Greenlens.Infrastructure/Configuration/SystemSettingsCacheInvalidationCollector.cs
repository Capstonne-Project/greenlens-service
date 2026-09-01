using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Infrastructure.Configuration;

internal sealed class SystemSettingsCacheInvalidationCollector : ISystemSettingsCacheInvalidationCollector
{
    private int _scheduled;

    public void Schedule() => Interlocked.Exchange(ref _scheduled, 1);

    public bool TryConsumeScheduled() => Interlocked.CompareExchange(ref _scheduled, 0, 1) == 1;

    public void Clear() => Interlocked.Exchange(ref _scheduled, 0);
}
