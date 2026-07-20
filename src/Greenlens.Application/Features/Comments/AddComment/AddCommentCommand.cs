using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Comments.AddComment;

public sealed record AddCommentImageItem(string Url, string MimeType, long SizeBytes);

/// <summary>Post a comment (or reply) on a report. BR-CMT-001..003.</summary>
public sealed record AddCommentCommand(
    Guid ReportId,
    string Content,
    IReadOnlyList<AddCommentImageItem>? Images = null,
    Guid? ParentCommentId = null) : IRequest<Result<AddCommentResponse>>;

public sealed record AddCommentResponse(
    Guid Id,
    Guid ReportId,
    string Content,
    DateTime CreatedAt,
    bool CanEdit,
    Guid? ParentCommentId,
    IReadOnlyList<AddCommentImageDto> Images);

public sealed record AddCommentImageDto(string Url, string MimeType, long SizeBytes);
