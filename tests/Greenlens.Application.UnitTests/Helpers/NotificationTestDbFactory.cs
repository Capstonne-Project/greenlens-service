using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.UnitTests.Helpers;

/// <summary>
/// In-memory ApplicationDbContext for notification locality enrichment in unit tests.
/// </summary>
internal static class NotificationTestDbFactory
{
    internal static IApplicationDbContext CreateEmpty()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"notification-tests-{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}
