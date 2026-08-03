using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Comments.GetReportComments;

/// <summary>List paginated comments for a report. Hidden comments omitted for citizens.</summary>
public sealed class GetReportCommentsQueryHandler(
    IReportRepository reports,
    IApplicationDbContext db,
    ICurrentUser currentUser,
    ILogger<GetReportCommentsQueryHandler> logger)
    : IRequestHandler<GetReportCommentsQuery, Result<GetReportCommentsResponse>>
{
    public async Task<Result<GetReportCommentsResponse>> Handle(
        GetReportCommentsQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting report comments");

        var reportExists = await reports.ExistsAsync(r => r.Id == request.ReportId, ct)
            .ConfigureAwait(false);
        if (!reportExists)
        {
            logger.LogWarning("Report not found for report {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }
        var isPrivileged = currentUser.IsAuthenticated && CommentAccess.IsPrivilegedRole(currentUser.Role);
        var userId = currentUser.IsAuthenticated ? currentUser.UserId : Guid.Empty;

        var query = db.Set<Comment>().AsNoTracking()
            .Where(c => c.ReportId == request.ReportId);

        if (!isPrivileged)
        {
            logger.LogWarning("Comments not hidden for privileged user {UserId}", currentUser.UserId);
            query = query.Where(c => !c.IsHidden);
        }

        var total = await query.CountAsync(ct).ConfigureAwait(false);

        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new
            {
                c.Id,
                c.Content,
                c.AuthorId,
                AuthorFullName = c.Author.FullName,
                AuthorRole = c.Author.Role.ToString(),
                AuthorAvatarUrl = c.Author.AvatarUrl,
                c.CreatedAt,
                c.UpdatedAt,
                c.IsHidden,
                c.ParentCommentId,
                LikeCount = c.Likes.Count,
                LikedByMe = userId != Guid.Empty && c.Likes.Any(l => l.UserId == userId),
                Images = c.Media.Select(m => new CommentImageItem(m.Url, m.MimeType, m.SizeBytes)).ToList()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = rows.Select(r =>
        {
            var isAuthor = currentUser.IsAuthenticated && r.AuthorId == currentUser.UserId;
            var withinWindow = DateTime.UtcNow - r.CreatedAt <= TimeSpan.FromMinutes(15);
            var authorName = CommentAccess.ResolveAuthorDisplayName(r.AuthorRole, r.AuthorFullName);
            // BR-CMT-001: đội xử lý hiện nhãn chung → không lộ avatar cá nhân.
            var authorAvatarUrl = CommentAccess.IsCleanupTeamRole(r.AuthorRole) ? null : r.AuthorAvatarUrl;
            return new CommentListItem(
                r.Id, r.Content, authorName, r.AuthorId, authorAvatarUrl,
                r.CreatedAt, r.UpdatedAt, r.IsHidden,
                isAuthor && withinWindow && !r.IsHidden,
                isAuthor && withinWindow,
                r.ParentCommentId,
                r.LikeCount,
                r.LikedByMe,
                r.Images);
        }).ToList();

        logger.LogInformation("Report comments: {Items}", items);

        return new GetReportCommentsResponse(
            items,
            PaginationMeta.Create(request.Page, request.PageSize, total));
    }
}
