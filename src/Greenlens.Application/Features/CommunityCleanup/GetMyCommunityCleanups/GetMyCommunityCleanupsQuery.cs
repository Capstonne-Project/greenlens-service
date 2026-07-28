using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetMyCommunityCleanups;

/// <summary>Citizen's own joined/led programs.</summary>
public sealed record GetMyCommunityCleanupsQuery(int Page, int PageSize) : IRequest<Result<CommunityCleanupListResponse>>;
