using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.BlockedWords.DeleteBlockedWord;

public sealed class DeleteBlockedWordCommandHandler(
    IBlockedWordRepository blockedWords,
    IBlockedWordCache cache,
    IAuditLogger auditLogger,
    IUnitOfWork uow,
    ILogger<DeleteBlockedWordCommandHandler> logger)
    : IRequestHandler<DeleteBlockedWordCommand, Result>
{
    public async Task<Result> Handle(DeleteBlockedWordCommand request, CancellationToken ct)
    {
        var entity = await blockedWords.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (entity is null)
        {
            logger.LogWarning("Blocked word not found: {Id}", request.Id);
            return Errors.BlockedWords.NotFound;
        }

        if (!entity.IsActive)
        {
            logger.LogWarning("Blocked word is already deactivated: {Id}", request.Id);
            return Result.Success();
        }

        var oldSnapshot = JsonSerializer.Serialize(new { entity.Word, entity.Note, entity.IsActive });

        entity.Deactivate();
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RefreshAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "BlockedWord.Deactivate",
            "BlockedWord",
            entity.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new { entity.Word, entity.Note, entity.IsActive }),
            ct).ConfigureAwait(false);

        logger.LogInformation("Blocked word deactivated {BlockedWordId}", entity.Id);

        return Result.Success();
    }
}
