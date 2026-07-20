using Greenlens.Application.Features.Organization.GetTeamById;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetMyTeamProfile;

public sealed record GetMyTeamProfileQuery : IRequest<Result<TeamDetailResponse>>;
