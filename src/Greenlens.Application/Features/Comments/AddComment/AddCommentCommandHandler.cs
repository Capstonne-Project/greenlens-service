using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Comments.AddComment;

/// <summary>
/// Citizen / cleanup team posts a comment (or reply) on a report.
/// </summary>
/// <remarks>
/// Implements: BR-CMT-001 (auth + anonymous report guard), BR-CMT-002 (length/images),
/// BR-CMT-003 (word filter + 3-strike ban).
/// </remarks>
public sealed class AddCommentCommandHandler(
    IReportRepository reports,
    IUserRepository users,
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IProfanityFilter profanityFilter,
    IUnitOfWork uow,
    ILogger<AddCommentCommandHandler> logger)
    : IRequestHandler<AddCommentCommand, Result<AddCommentResponse>>
{
    public async Task<Result<AddCommentResponse>> Handle(AddCommentCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting add comment");

        if (!currentUser.IsAuthenticated)
        {
            logger.LogWarning("Add comment attempt by unauthenticated user");
            return Errors.Comments.LoginRequired;
        }

        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("User not found for user {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        if (user.IsCommentBanned())
        {
            logger.LogWarning("User {UserId} is comment banned", currentUser.UserId);
            return Errors.Comments.CommentBanned;
        }

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for report {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (!CommentAccess.CanCommentOnReport(
                report.HideReporterName, currentUser.Role, currentUser.UserId, report.ReporterId))
        {
            logger.LogWarning("Comment not allowed for report {ReportId}", request.ReportId);
            return Errors.Comments.CommentNotAllowed;
        }

        Guid? parentId = request.ParentCommentId;
        if (parentId is not null)
        {
            var parent = await db.Set<Comment>().AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == parentId.Value && c.ReportId == request.ReportId, ct)
                .ConfigureAwait(false);

            if (parent is null)
            {
                logger.LogWarning("Parent comment not found for comment {ParentCommentId}", parentId.Value);
                return Errors.Comments.CommentNotFound;
            }

            // Flatten nested replies to one level under the root parent (TikTok-style).
            parentId = parent.ParentCommentId ?? parent.Id;
        }

        if (profanityFilter.ContainsProfanity(request.Content))
        {
            user.RecordCommentViolation();
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogWarning("Inappropriate content detected for user {UserId}", currentUser.UserId);
            return Errors.Comments.InappropriateContent;
        }

        Comment comment;
        try
        {
            comment = Comment.Create(request.ReportId, currentUser.UserId, request.Content, parentId);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Domain validation error for comment {ReportId}: {Message}", request.ReportId, ex.Message);
            return Errors.Comments.DomainValidation(ex.Message);
        }

        comment.AddDomainEvent(new CommentPostedEvent(
            comment.Id, report.Id, currentUser.UserId, report.ReporterId));

        logger.LogInformation("Comment created: {Comment}", comment);

        db.Set<Comment>().Add(comment);

        var imageDtos = new List<AddCommentImageDto>();
        if (request.Images is { Count: > 0 })
        {
            foreach (var img in request.Images)
            {
                var media = CommentMedia.Create(comment.Id, img.Url.Trim(), img.MimeType.Trim(), img.SizeBytes);
                db.Set<CommentMedia>().Add(media);
                imageDtos.Add(new AddCommentImageDto(media.Url, media.MimeType, media.SizeBytes));
            }
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Comment {CommentId} added on report {ReportId} by {UserId} (parent={ParentId})",
            comment.Id, report.Id, currentUser.UserId, parentId);

        return new AddCommentResponse(
            comment.Id, comment.ReportId, comment.Content, comment.CreatedAt,
            comment.IsWithinEditWindow(), comment.ParentCommentId, imageDtos);
    }
}
