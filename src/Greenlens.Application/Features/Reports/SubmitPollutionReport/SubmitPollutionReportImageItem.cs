namespace Greenlens.Application.Features.Reports.SubmitPollutionReport;

/// <summary>
/// One image already uploaded via POST /v1/media/reports/images; client sends public URL and metadata here.
/// </summary>
public sealed record SubmitPollutionReportImageItem(
    string Url,
    string MimeType,
    long SizeBytes);
