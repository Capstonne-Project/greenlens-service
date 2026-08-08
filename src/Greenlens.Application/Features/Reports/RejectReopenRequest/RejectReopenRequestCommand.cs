using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.RejectReopenRequest;

public sealed record RejectReopenRequestCommand(
    Guid ReportId,
    Guid RequestId,
    string Reason) : IRequest<Result>;
