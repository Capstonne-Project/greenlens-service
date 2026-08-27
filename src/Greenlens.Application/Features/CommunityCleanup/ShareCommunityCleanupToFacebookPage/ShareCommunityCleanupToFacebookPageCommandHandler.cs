using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Options;
using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Application.Features.CommunityCleanup.ShareCommunityCleanupToFacebookPage;

/// <summary>
/// Publishes a community cleanup event to the configured Facebook Page when LEO clicks share.
/// </summary>
public sealed class ShareCommunityCleanupToFacebookPageCommandHandler(
    ICommunityCleanupEventRepository events,
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IOptions<PublicWebOptions> publicWebOptions,
    IOptions<MetaPageOptions> metaPageOptions,
    IFacebookPagePublisher facebookPagePublisher,
    ILogger<ShareCommunityCleanupToFacebookPageCommandHandler> logger)
    : IRequestHandler<ShareCommunityCleanupToFacebookPageCommand, Result<CommunityCleanupFacebookAutoPostDto>>
{
    public async Task<Result<CommunityCleanupFacebookAutoPostDto>> Handle(
        ShareCommunityCleanupToFacebookPageCommand request,
        CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        return await CommunityCleanupFacebookPageShare.PublishAsync(
            ev,
            reports,
            reportMedia,
            publicWebOptions,
            metaPageOptions,
            facebookPagePublisher,
            logger,
            ct).ConfigureAwait(false);
    }
}
