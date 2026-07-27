using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Features.Gamification.EventHandlers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class LevelUpNotificationHandlerTests
{
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly LevelUpNotificationHandler _sut;

    public LevelUpNotificationHandlerTests()
    {
        _sut = new LevelUpNotificationHandler(
            _notifications,
            NullLogger<LevelUpNotificationHandler>.Instance);
    }

    [Fact]
    public async Task Handle_LevelIncreased_SendsLevelUpTemplate_BR_GAM_003()
    {
        var userId = Guid.NewGuid();
        var evt = new LevelUpEvent(userId, PreviousLevel: 1, NewLevel: 2);

        await _sut.Handle(evt, CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            userId,
            NotificationType.LevelUp,
            Arg.Is<Dictionary<string, string>>(d => d["level"] == "2"),
            userId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoLevelChange_DoesNotSendNotification_BR_GAM_003()
    {
        var evt = new LevelUpEvent(Guid.NewGuid(), PreviousLevel: 2, NewLevel: 2);

        await _sut.Handle(evt, CancellationToken.None);

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            Arg.Any<Guid>(),
            Arg.Any<NotificationType>(),
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }
}
