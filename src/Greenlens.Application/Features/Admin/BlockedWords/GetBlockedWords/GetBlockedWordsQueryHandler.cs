using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.BlockedWords.GetBlockedWords;

public sealed class GetBlockedWordsQueryHandler(IApplicationDbContext db, ILogger<GetBlockedWordsQueryHandler> logger)
    : IRequestHandler<GetBlockedWordsQuery, Result<GetBlockedWordsResponse>>
{   
    public async Task<Result<GetBlockedWordsResponse>> Handle(GetBlockedWordsQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting blocked words");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.Set<BlockedWord>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(w => w.Word.Contains(term));
            logger.LogInformation("Search term: {Term}", term);
        }

        if (request.IsActive is not null)
        {
            query = query.Where(w => w.IsActive == request.IsActive);
            logger.LogInformation("Is active: {IsActive}", request.IsActive);
        }

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Total count of blocked words: {Total}", total);

        var items = await query
            .OrderBy(w => w.Word)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new BlockedWordItem(w.Id, w.Word, w.Note, w.IsActive, w.CreatedAt, w.UpdatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Blocked words retrieved successfully");

        return new GetBlockedWordsResponse(items, total);
    }
}
