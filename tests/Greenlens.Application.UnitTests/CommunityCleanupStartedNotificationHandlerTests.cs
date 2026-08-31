using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications.EventHandlers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class CommunityCleanupStartedNotificationHandlerTests
{
    private readonly ICommunityCleanupParticipantRepository _participants = Substitute.For<ICommunityCleanupParticipantRepository>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly CommunityCleanupStartedNotificationHandler _sut;

    public CommunityCleanupStartedNotificationHandlerTests()
    {
        _sut = new CommunityCleanupStartedNotificationHandler(
            _participants,
            _notifications,
            NullLogger<CommunityCleanupStartedNotificationHandler>.Instance);
    }

    [Fact]
    public async Task Handle_JoinedMembersReceiveStartedNotification_ExcludesLeader_BR_CMU_006()
    {
        var eventId = Guid.NewGuid();
        var leaderId = Guid.NewGuid();
        var joinedMember = Guid.NewGuid();
        var checkedInMember = Guid.NewGuid();
        var leoId = Guid.NewGuid();

        var rows = new List<CommunityCleanupParticipant>
        {
            CommunityCleanupParticipant.Create(eventId, leaderId, CommunityCleanupParticipantRole.Leader),
            CommunityCleanupParticipant.Create(eventId, joinedMember, CommunityCleanupParticipantRole.Member),
            CommunityCleanupParticipant.Create(eventId, checkedInMember, CommunityCleanupParticipantRole.Member)
        };
        rows.Single(p => p.UserId == checkedInMember).CheckIn(10.78m, 106.69m);

        _participants.GetByEventIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(rows);

        var evt = new CommunityCleanupStartedEvent(eventId, Guid.NewGuid(), "Dọn rác Hiệp Bình", leaderId, leoId);

        await _sut.Handle(evt, CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            joinedMember,
            NotificationType.CommunityCleanupStarted,
            Arg.Is<Dictionary<string, string>>(d => d["title"] == "Dọn rác Hiệp Bình"),
            eventId,
            Arg.Any<CancellationToken>());

        await _notifications.Received(1).SendFromTemplateAsync(
            leoId,
            NotificationType.CommunityCleanupLeaderStarted,
            Arg.Is<Dictionary<string, string>>(d => d["title"] == "Dọn rác Hiệp Bình"),
            eventId,
            Arg.Any<CancellationToken>());

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            leaderId,
            NotificationType.CommunityCleanupStarted,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            checkedInMember,
            NotificationType.CommunityCleanupStarted,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LeoReceivesLeaderStartedNotification_BR_CMU_006()
    {
        var eventId = Guid.NewGuid();
        var leaderId = Guid.NewGuid();
        var leoId = Guid.NewGuid();

        _participants.GetByEventIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns([CommunityCleanupParticipant.Create(eventId, leaderId, CommunityCleanupParticipantRole.Leader)]);

        var evt = new CommunityCleanupStartedEvent(eventId, Guid.NewGuid(), "Dọn rác Hiệp Bình", leaderId, leoId);

        await _sut.Handle(evt, CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            leoId,
            NotificationType.CommunityCleanupLeaderStarted,
            Arg.Is<Dictionary<string, string>>(d => d["title"] == "Dọn rác Hiệp Bình"),
            eventId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoJoinedMembers_StillNotifiesLeo_BR_CMU_006()
    {
        var eventId = Guid.NewGuid();
        var leaderId = Guid.NewGuid();
        var leoId = Guid.NewGuid();

        _participants.GetByEventIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns([CommunityCleanupParticipant.Create(eventId, leaderId, CommunityCleanupParticipantRole.Leader)]);

        var evt = new CommunityCleanupStartedEvent(eventId, Guid.NewGuid(), "Dọn rác Hiệp Bình", leaderId, leoId);

        await _sut.Handle(evt, CancellationToken.None);

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            Arg.Any<Guid>(),
            NotificationType.CommunityCleanupStarted,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());

        await _notifications.Received(1).SendFromTemplateAsync(
            leoId,
            NotificationType.CommunityCleanupLeaderStarted,
            Arg.Any<Dictionary<string, string>>(),
            eventId,
            Arg.Any<CancellationToken>());
    }
}
