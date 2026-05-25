using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.UploadProgressImage;

/// <summary>Upload a progress image for an in-progress assignment. Returns the public URL.</summary>
public sealed record UploadProgressImageCommand(
    Guid ReportId,
    Guid TeamId,
    byte[] ImageBytes,
    string FileName,
    string ContentType) : IRequest<Result<UploadProgressImageResponse>>;

public sealed record UploadProgressImageResponse(string ImageUrl);
