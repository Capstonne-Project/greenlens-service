using Greenlens.Application.Features.Reports.AnalyzeReportImage;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.AnalyzeUploadedReportImage;

/// <summary>
/// Analyze a report image already uploaded directly to R2.
/// </summary>
public sealed record AnalyzeUploadedReportImageCommand(
    string PublicUrl,
    string Key,
    string FileName,
    string ContentType,
    long SizeBytes) : IRequest<Result<AnalyzeReportImageResponse>>, INoTransaction;
