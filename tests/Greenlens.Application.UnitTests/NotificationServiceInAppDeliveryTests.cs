using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.UnitTests.TestDoubles;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Notifications;
using Greenlens.Infrastructure.Notifications.Hubs;
using Greenlens.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

/// <summary>
/// BR-NTF-001: non-deliverable email must not block in-app notification persistence + SignalR.
/// </summary>
public sealed class NotificationServiceInAppDeliveryTests
{
    [Fact]
    public async Task SendRawAsync_NonDeliverableEmailWithPushDisabled_StillPersistsInApp_BR_NTF_001()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"notif-inapp-{Guid.NewGuid():N}")
            .Options;
        await using var db = new ApplicationDbContext(options);

        var user = User.Create("deo.79@greenlens.dev", "hash", "Seed DEO", UserRole.DEO);
        db.Users.Add(user);
        db.NotificationPreferences.Add(NotificationPreference.Create(
            user.Id, NotificationType.ReportStatusChanged, pushEnabled: false, emailEnabled: true));
        await db.SaveChangesAsync();

        var hubContext = Substitute.For<IHubContext<NotificationHub, INotificationClient>>();
        var clientProxy = Substitute.For<INotificationClient>();
        var clients = Substitute.For<IHubClients<INotificationClient>>();
        hubContext.Clients.Returns(clients);
        clients.User(user.Id.ToString()).Returns(clientProxy);

        var dispatchScheduler = Substitute.For<INotificationDispatchScheduler>();
        var dispatchCollector = Substitute.For<INotificationDispatchCollector>();
        var transactionManager = Substitute.For<ITransactionManager>();
        var changeTrackerCleaner = Substitute.For<IChangeTrackerCleaner>();

        var sut = new NotificationService(
            db,
            changeTrackerCleaner,
            dispatchScheduler,
            dispatchCollector,
            transactionManager,
            hubContext,
            new DefaultSystemSettingsProvider(),
            NullLogger<NotificationService>.Instance);

        await sut.SendRawAsync(
            user.Id,
            NotificationType.ReportStatusChanged,
            "Test title",
            "Test message",
            referenceId: null,
            CancellationToken.None);

        (await db.Notifications.CountAsync()).Should().Be(1);
        await clientProxy.Received(1).ReceiveNotification(Arg.Any<RealTimeNotificationPayload>());
        dispatchScheduler.DidNotReceive().Enqueue(Arg.Any<Guid>());
        dispatchCollector.DidNotReceive().Enqueue(Arg.Any<Guid>());
    }
}
