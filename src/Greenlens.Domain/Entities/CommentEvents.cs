using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>Raised when a citizen posts a comment on a report. BR-NTF-002.</summary>
public sealed record CommentPostedEvent(Guid CommentId, Guid ReportId, Guid AuthorId, Guid? ReporterId)
    : IDomainEvent;
