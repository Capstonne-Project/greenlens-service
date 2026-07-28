using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetCommunityCleanupById;

public sealed record GetCommunityCleanupByIdQuery(Guid EventId) : IRequest<Result<CommunityCleanupEventDetailResponse>>;
