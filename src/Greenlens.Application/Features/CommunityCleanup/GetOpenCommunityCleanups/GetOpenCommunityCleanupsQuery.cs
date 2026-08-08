using Greenlens.Application.Features.CommunityCleanup.Common;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.GetOpenCommunityCleanups;

/// <summary>Citizen browses OpenForJoin programs, optionally filtered by proximity.</summary>
public sealed record GetOpenCommunityCleanupsQuery(
    int Page,
    int PageSize,
    decimal? NearLat,
    decimal? NearLng,
    double? RadiusMeters) : IRequest<Result<CommunityCleanupListResponse>>;
