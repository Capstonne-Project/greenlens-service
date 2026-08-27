using FluentAssertions;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Options;
using Greenlens.Application.Features.CommunityCleanup.ShareCommunityCleanupToFacebookPage;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Greenlens.Application.UnitTests;

public sealed class ShareCommunityCleanupToFacebookPageCommandHandlerTests
{
    [Fact]
    public async Task Handle_EventNotFound_ReturnsNotFound()
    {
        var events = Substitute.For<ICommunityCleanupEventRepository>();
        events.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((CommunityCleanupEvent?)null);

        var handler = CreateHandler(events: events);

        var result = await handler.Handle(new ShareCommunityCleanupToFacebookPageCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("COMMUNITY_EVENT_NOT_FOUND");
    }

    [Fact]
    public async Task Handle_FeatureDisabled_ReturnsBusinessRule()
    {
        var ev = CreateEvent();
        var events = Substitute.For<ICommunityCleanupEventRepository>();
        events.GetByIdAsync(ev.Id, Arg.Any<CancellationToken>()).Returns(ev);

        var handler = CreateHandler(
            events: events,
            meta: new MetaPageOptions { AutoPostEnabled = false });

        var result = await handler.Handle(new ShareCommunityCleanupToFacebookPageCommand(ev.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("META_PAGE_SHARE_DISABLED");
    }

    private static ShareCommunityCleanupToFacebookPageCommandHandler CreateHandler(
        ICommunityCleanupEventRepository? events = null,
        MetaPageOptions? meta = null)
    {
        return new ShareCommunityCleanupToFacebookPageCommandHandler(
            events ?? Substitute.For<ICommunityCleanupEventRepository>(),
            Substitute.For<IReportRepository>(),
            Substitute.For<IReportMediaRepository>(),
            Options.Create(new PublicWebOptions { BaseUrl = "https://portal.test" }),
            Options.Create(meta ?? new MetaPageOptions
            {
                AutoPostEnabled = true,
                PageId = "642977455573500",
                PageAccessToken = "token"
            }),
            Substitute.For<IFacebookPagePublisher>(),
            NullLogger<ShareCommunityCleanupToFacebookPageCommandHandler>.Instance);
    }

    private static CommunityCleanupEvent CreateEvent() =>
        CommunityCleanupEvent.Create(
            reportId: Guid.NewGuid(),
            createdByLeoId: Guid.NewGuid(),
            leaderUserId: Guid.NewGuid(),
            leaderTeamId: Guid.NewGuid(),
            title: "Test",
            description: null,
            startsAt: DateTime.UtcNow.AddDays(1),
            endsAt: null,
            joinClosesAt: null,
            maxParticipants: 50,
            meetingNote: null,
            meetingLatitude: null,
            meetingLongitude: null);
}
