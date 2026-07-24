using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.BlockedWords.UpdateBlockedWord;

public sealed class UpdateBlockedWordCommandHandler(
    IBlockedWordRepository blockedWords,
    IBlockedWordCache cache,
    IAuditLogger auditLogger,
    IUnitOfWork uow,
    ILogger<UpdateBlockedWordCommandHandler> logger)
    : IRequestHandler<UpdateBlockedWordCommand, Result>
{
    public async Task<Result> Handle(UpdateBlockedWordCommand request, CancellationToken ct)
    {
        var entity = await blockedWords.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (entity is null)
        {
            logger.LogWarning("Blocked word not found: {Id}", request.Id);
            return Errors.BlockedWords.NotFound;
        }

        var normalized = BlockedWord.NormalizeWord(request.Word);
        if (await blockedWords.ExistsWordAsync(normalized, request.Id, ct).ConfigureAwait(false))
        {
            logger.LogWarning("Blocked word already exists: {Word}", request.Word);
            return Errors.BlockedWords.Duplicate;
        }

        var oldSnapshot = JsonSerializer.Serialize(new { entity.Word, entity.Note, entity.IsActive });

        entity.Update(request.Word, request.Note, request.IsActive);
        logger.LogInformation("Updating blocked word: {Word}", request.Word);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RefreshAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "BlockedWord.Update",
            "BlockedWord",
            entity.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new { entity.Word, entity.Note, entity.IsActive }),
            ct).ConfigureAwait(false);

        logger.LogInformation("Blocked word updated {BlockedWordId}", entity.Id);

        return Result.Success();
    }
}
