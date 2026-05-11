using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.SubmitPollutionReport;

public sealed record SubmitPollutionReportCommand(
    Guid CategoryId,
    Severity Severity,
    string? Description,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    string? WardCode,
    string? ProvinceCode,
    bool IsAnonymous,
    IReadOnlyList<SubmitPollutionReportImageItem> Images) : IRequest<Result<SubmitPollutionReportResponse>>;
