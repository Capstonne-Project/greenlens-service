using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminAlerts;

public sealed record GetAdminAlertsQuery : IRequest<Result<List<AlertItem>>>;

public sealed record AlertItem(
    string Type,
    string Severity,
    string Message);
