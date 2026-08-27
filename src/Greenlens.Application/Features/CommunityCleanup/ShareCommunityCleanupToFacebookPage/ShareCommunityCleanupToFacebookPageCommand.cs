using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.ShareCommunityCleanupToFacebookPage;

public sealed record ShareCommunityCleanupToFacebookPageCommand(Guid EventId)
    : IRequest<Result<CommunityCleanupFacebookAutoPostDto>>;
