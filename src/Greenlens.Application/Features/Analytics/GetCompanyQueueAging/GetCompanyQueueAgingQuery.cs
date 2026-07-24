using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetCompanyQueueAging;

public sealed record GetCompanyQueueAgingQuery(
    DateTime? From = null,
    DateTime? To = null) : IRequest<Result<List<CompanyQueueAgingBucket>>>;

public sealed record CompanyQueueAgingBucket(
    string Range,
    int Count);
