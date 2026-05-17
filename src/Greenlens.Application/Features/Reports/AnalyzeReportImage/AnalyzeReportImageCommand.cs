using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.AnalyzeReportImage;

/// <summary>
/// Step 1 of the report flow: analyze an image via AI Service, store temporarily, return temp ID + AI result.
/// </summary>
/// <remarks>
/// Implements: BR-AI-001, BR-AI-006 (timeout fallback), BR-AI-007 (EXIF strip in infra).
/// Ảnh chưa được lưu vĩnh viễn — chỉ lưu temp 15 phút.
/// </remarks>
public sealed record AnalyzeReportImageCommand(
    byte[] ImageBytes,
    string FileName,
    string ContentType,
    long FileSize) : IRequest<Result<AnalyzeReportImageResponse>>;
