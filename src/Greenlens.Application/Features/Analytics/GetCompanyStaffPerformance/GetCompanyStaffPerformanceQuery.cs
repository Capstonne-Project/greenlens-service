using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetCompanyStaffPerformance;

public sealed record GetCompanyStaffPerformanceQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<StaffPerformanceItem>>>;

public sealed record StaffPerformanceItem(
    Guid StaffId,
    string StaffName,
    int TasksHandled,
    int TasksCompleted,
    decimal CompletionRate);
