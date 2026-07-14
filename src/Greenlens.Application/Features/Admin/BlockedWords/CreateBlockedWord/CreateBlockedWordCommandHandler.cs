using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.BlockedWords.CreateBlockedWord;

public sealed class CreateBlockedWordCommandHandler(
    IBlockedWordRepository blockedWords,
    IBlockedWordCache cache,
    IAuditLogger auditLogger,
    IUnitOfWork uow,
    ILogger<CreateBlockedWordCommandHandler> logger)
    : IRequestHandler<CreateBlockedWordCommand, Result<CreateBlockedWordResponse>>
{
    public async Task<Result<CreateBlockedWordResponse>> Handle(CreateBlockedWordCommand request, CancellationToken ct)
    {
        var normalized = BlockedWord.NormalizeWord(request.Word);

        if (await blockedWords.ExistsWordAsync(normalized, excludeId: null, ct).ConfigureAwait(false))
            return Errors.BlockedWords.Duplicate;

        var entity = BlockedWord.Create(request.Word, request.Note);
        blockedWords.Add(entity);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await cache.RefreshAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "BlockedWord.Create",
            "BlockedWord",
            entity.Id.ToString(),
            oldValues: null,
            newValues: JsonSerializer.Serialize(new { entity.Word, entity.Note, entity.IsActive }),
            ct).ConfigureAwait(false);

        logger.LogInformation("Blocked word created {BlockedWordId}", entity.Id);

        return new CreateBlockedWordResponse(entity.Id, entity.Word, entity.IsActive);
    }
}
