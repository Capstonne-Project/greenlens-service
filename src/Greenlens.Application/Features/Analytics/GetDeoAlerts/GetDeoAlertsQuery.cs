using Greenlens.Application.Features.Analytics.GetAdminAlerts;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoAlerts;

public sealed record GetDeoAlertsQuery : IRequest<Result<List<AlertItem>>>;
