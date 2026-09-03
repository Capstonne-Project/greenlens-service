using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Notifications.EventHandlers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class CommunityCleanupBeforeImagesUploadedNotificationHandlerTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly CommunityCleanupBeforeImagesUploadedNotificationHandler _sut;

    public CommunityCleanupBeforeImagesUploadedNotificationHandlerTests()
    {
        _sut = new CommunityCleanupBeforeImagesUploadedNotificationHandler(
            _notifications,
            NullLogger<CommunityCleanupBeforeImagesUploadedNotificationHandler>.Instance);
    }

    [Fact]
    public async Task Handle_LeaderUploadsBeforeImages_LeoReceivesNotification_BR_CMU_008()
    {
        var eventId = Guid.NewGuid();
        var leoId = Guid.NewGuid();
        var evt = new CommunityCleanupBeforeImagesUploadedEvent(eventId, "Dọn rác Hiệp Bình", leoId, 3);

        await _sut.Handle(evt, CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            leoId,
            NotificationType.CommunityCleanupBeforeImagesUploaded,
            Arg.Is<Dictionary<string, string>>(d =>
                d["title"] == "Dọn rác Hiệp Bình" && d["image_count"] == "3"),
            eventId,
            Arg.Any<CancellationToken>());
    }
}
