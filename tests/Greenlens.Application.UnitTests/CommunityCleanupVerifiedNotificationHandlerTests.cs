using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications.EventHandlers;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class CommunityCleanupVerifiedNotificationHandlerTests
{
    private readonly ICommunityCleanupParticipantRepository _participants = Substitute.For<ICommunityCleanupParticipantRepository>();
    private readonly INotificationService _notifications = Substitute.For<INotificationService>();
    private readonly CommunityCleanupVerifiedNotificationHandler _sut;

    public CommunityCleanupVerifiedNotificationHandlerTests()
    {
        _sut = new CommunityCleanupVerifiedNotificationHandler(
            _participants,
            _notifications,
            NullLogger<CommunityCleanupVerifiedNotificationHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CheckedInParticipantsNotified_ApprovingLeoExcluded_BR_CMU_010()
    {
        var eventId = Guid.NewGuid();
        var approvingLeoId = Guid.NewGuid();
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var joinedOnlyId = Guid.NewGuid();

        var rows = new List<CommunityCleanupParticipant>
        {
            CommunityCleanupParticipant.Create(eventId, leaderId, CommunityCleanupParticipantRole.Leader),
            CommunityCleanupParticipant.Create(eventId, memberId, CommunityCleanupParticipantRole.Member),
            CommunityCleanupParticipant.Create(eventId, joinedOnlyId, CommunityCleanupParticipantRole.Member)
        };
        rows.Single(p => p.UserId == leaderId).CheckIn(10.78m, 106.69m);
        rows.Single(p => p.UserId == memberId).CheckIn(10.78m, 106.69m);

        _participants.GetByEventIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(rows);

        var evt = new CommunityCleanupVerifiedEvent(eventId, "Dọn rác Hiệp Bình", approvingLeoId);

        await _sut.Handle(evt, CancellationToken.None);

        await _notifications.Received(1).SendFromTemplateAsync(
            leaderId,
            NotificationType.CommunityCleanupVerified,
            Arg.Is<Dictionary<string, string>>(d => d["title"] == "Dọn rác Hiệp Bình"),
            eventId,
            Arg.Any<CancellationToken>());

        await _notifications.Received(1).SendFromTemplateAsync(
            memberId,
            NotificationType.CommunityCleanupVerified,
            Arg.Any<Dictionary<string, string>>(),
            eventId,
            Arg.Any<CancellationToken>());

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            approvingLeoId,
            NotificationType.CommunityCleanupVerified,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());

        await _notifications.DidNotReceive().SendFromTemplateAsync(
            joinedOnlyId,
            NotificationType.CommunityCleanupVerified,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
    }
}
