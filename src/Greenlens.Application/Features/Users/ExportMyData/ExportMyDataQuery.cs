using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Users.ExportMyData;

/// <summary>
/// Export all personal data for the authenticated user (GDPR Article 20 / NĐ-13/2023/NĐ-CP).
/// </summary>
/// <remarks>Implements: BR-DAT-003 (right to access personal data).</remarks>
public sealed record ExportMyDataQuery(ExportMyDataFormat Format = ExportMyDataFormat.Json)
    : IRequest<Result<ExportMyDataResponse>>;

public enum ExportMyDataFormat
{
    Json,
    Csv
}

public sealed record ExportMyDataResponse(byte[] Content, string ContentType, string FileName);
