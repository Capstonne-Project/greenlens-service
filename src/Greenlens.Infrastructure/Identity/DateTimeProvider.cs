using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Infrastructure.Identity;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
