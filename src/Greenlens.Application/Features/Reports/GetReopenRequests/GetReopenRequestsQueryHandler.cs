using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetReopenRequests;

/// <summary>Returns paginated reopen requests for the current LEO's office (BR-REP-015).</summary>
/// <remarks>Implements: BR-REP-015, BR-ORG-012.</remarks>
public sealed class GetReopenRequestsQueryHandler(
    IApplicationDbContext db,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetReopenRequestsQueryHandler> logger) : IRequestHandler<GetReopenRequestsQuery, Result<GetReopenRequestsResponse>>
{
    public async Task<Result<GetReopenRequestsResponse>> Handle(
        GetReopenRequestsQuery request,
        CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        if (user.Role != UserRole.LEO && user.Role != UserRole.Admin)
        {
            logger.LogWarning("User {UserId} is not a LEO or Admin", currentUser.UserId);
            return Errors.Reports.ReopenReviewForbidden;
        }

        var query = db.Set<Domain.Entities.ReportReopenRequest>()
            .AsNoTracking()
            .Include(r => r.Report)
            .Include(r => r.Media)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        if (user.Role == UserRole.LEO)
        {
            if (!user.LocalOfficeId.HasValue)
            {
                logger.LogWarning("LEO {UserId} has no local office", currentUser.UserId);
                return Errors.Organization.OfficeNotFound;
            }

            query = query.Where(r => r.Report.AssignedOfficeId == user.LocalOfficeId);
        }

        query = query.OrderByDescending(r => r.RequestedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var rows = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var items = rows.Select(r =>
        {
            var images = r.Media
                .Where(m => m.Type is MediaType.ReopenEvidence or MediaType.Image)
                .ToList();
            var hasVideo = r.Media.Any(m => m.Type == MediaType.Video);

            return new ReopenRequestListItem(
                r.Id,
                r.ReportId,
                r.Report.Code,
                r.Report.Status,
                r.Reason,
                r.Status,
                r.RequestedAt,
                images.FirstOrDefault()?.Url,
                images.Count,
                hasVideo);
        }).ToList();

        logger.LogInformation("LEO {UserId} fetched {Count} reopen requests", currentUser.UserId, items.Count);

        return new GetReopenRequestsResponse(items, pagination);
    }
}
