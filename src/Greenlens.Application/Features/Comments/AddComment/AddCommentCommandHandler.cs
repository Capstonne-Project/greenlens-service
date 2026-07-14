using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Comments.AddComment;

/// <summary>
/// Citizen posts a comment on a report with optional image attachments.
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
        if (!currentUser.IsAuthenticated)
            return Errors.Comments.LoginRequired;

        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        if (user.IsCommentBanned())
            return Errors.Comments.CommentBanned;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        // BR-CMT-001: anonymous-display reports restrict who can comment
        if (!CommentAccess.CanCommentOnReport(
                report.HideReporterName, currentUser.Role, currentUser.UserId, report.ReporterId))
            return Errors.Comments.CommentNotAllowed;

        // BR-CMT-003: word filter (AI text moderation deferred)
        if (profanityFilter.ContainsProfanity(request.Content))
        {
            user.RecordCommentViolation();
            await uow.SaveChangesAsync(ct).ConfigureAwait(false);
            return Errors.Comments.InappropriateContent;
        }

        Comment comment;
        try
        {
            comment = Comment.Create(request.ReportId, currentUser.UserId, request.Content);
        }
        catch (DomainException ex)
        {
            return new Error("VALIDATION_ERROR", ex.Message, ErrorType.Validation);
        }

        comment.AddDomainEvent(new CommentPostedEvent(
            comment.Id, report.Id, currentUser.UserId, report.ReporterId));

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

        logger.LogInformation("Comment {CommentId} added on report {ReportId} by {UserId}",
            comment.Id, report.Id, currentUser.UserId);

        return new AddCommentResponse(
            comment.Id, comment.ReportId, comment.Content, comment.CreatedAt,
            comment.IsWithinEditWindow(), imageDtos);
    }
}
