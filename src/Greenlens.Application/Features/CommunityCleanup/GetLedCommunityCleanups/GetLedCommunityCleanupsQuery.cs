using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetLedCommunityCleanups;

/// <summary>Leader's "Chương trình tôi dẫn" list.</summary>
public sealed record GetLedCommunityCleanupsQuery(
    int Page,
    int PageSize,
    CommunityCleanupStatus? Status) : IRequest<Result<CommunityCleanupListResponse>>;
