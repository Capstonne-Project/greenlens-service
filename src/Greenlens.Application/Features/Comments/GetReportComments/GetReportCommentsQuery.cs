using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Comments.GetReportComments;

public sealed record GetReportCommentsQuery(
    Guid ReportId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<GetReportCommentsResponse>>;

public sealed record GetReportCommentsResponse(
    IReadOnlyList<CommentListItem> Items,
    PaginationMeta Pagination);

public sealed record CommentListItem(
    Guid Id,
    string Content,
    string AuthorName,
    Guid AuthorId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsHidden,
    bool CanEdit,
    bool CanDelete,
    IReadOnlyList<CommentImageItem> Images);

public sealed record CommentImageItem(string Url, string MimeType, long SizeBytes);
