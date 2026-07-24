using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetCompanyTaskStatus;

public sealed record GetCompanyTaskStatusQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<TaskStatusItem>>>;

public sealed record TaskStatusItem(
    AssignmentStatus Status,
    int Count,
    decimal Percentage);
