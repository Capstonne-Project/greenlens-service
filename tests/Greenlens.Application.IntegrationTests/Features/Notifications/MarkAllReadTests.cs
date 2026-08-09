using FluentAssertions;
using Greenlens.Application.Features.Notifications.MarkAllRead;
using Greenlens.Application.IntegrationTests.Fixtures;
using Greenlens.Application.IntegrationTests.Helpers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.IntegrationTests.Features.Notifications;

[Collection("Postgres")]
public sealed class MarkAllReadTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task MarkAllRead_UserA_DoesNotAffectUserBUnread_BR_NTF_001_TC_NTF_042()
    {
        var (userAId, userBId) = await WithDbAsync(async db =>
        {
            var userA = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.Citizen);
            var userB = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.Citizen);

            for (var i = 0; i < 5; i++)
            {
                db.Set<Notification>().Add(Notification.Create(
                    userA.Id,
                    NotificationType.ReportStatusChanged,
                    $"A-{i}",
                    "Message A"));
            }

            for (var i = 0; i < 3; i++)
            {
                db.Set<Notification>().Add(Notification.Create(
                    userB.Id,
                    NotificationType.ReportStatusChanged,
                    $"B-{i}",
                    "Message B"));
            }

            await db.SaveChangesAsync();
            return (userA.Id, userB.Id);
        });

        CurrentUser.UserId = userAId;
        var result = await Mediator.Send(new MarkAllReadCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.MarkedCount.Should().Be(5);

        await WithDbAsync(async db =>
        {
            var userAUnread = await db.Set<Notification>()
                .CountAsync(n => n.RecipientId == userAId && !n.IsRead);
            var userBUnread = await db.Set<Notification>()
                .CountAsync(n => n.RecipientId == userBId && !n.IsRead);

            userAUnread.Should().Be(0);
            userBUnread.Should().Be(3);
        });
    }

    [Fact]
    public async Task MarkAllRead_NoUnread_ReturnsZero_BR_NTF_001_TC_NTF_014()
    {
        var userId = await WithDbAsync(async db =>
        {
            var user = await IntegrationDataSeeder.SeedUserAsync(db, UserRole.Citizen);

            var notification = Notification.Create(
                user.Id,
                NotificationType.ReportStatusChanged,
                "Already read",
                "Message");
            notification.MarkAsRead();
            db.Set<Notification>().Add(notification);
            await db.SaveChangesAsync();
            return user.Id;
        });

        CurrentUser.UserId = userId;
        var result = await Mediator.Send(new MarkAllReadCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value!.MarkedCount.Should().Be(0);
    }
}
