using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.ExportReports;

/// <summary>
/// BR-OFF-022: Export reports as CSV or Excel.
/// LEO exports own ward, DEO exports province, Admin exports all.
/// PII (reporter name/phone) excluded unless Admin.
/// </summary>
public sealed record ExportReportsQuery(
    ReportStatus? Status = null,
    Severity? Severity = null,
    Guid? CategoryId = null,
    string? WardCode = null,
    DateTime? From = null,
    DateTime? To = null,
    bool? IsPossibleDuplicate = null,
    bool? IsSuspectedViolationRecurrence = null,
    ExportFormat Format = ExportFormat.Csv) : IRequest<Result<ExportReportsResponse>>;

public enum ExportFormat
{
    Csv,
    Excel
}

public sealed record ExportReportsResponse(
    byte[] Content,
    string ContentType,
    string FileName);
