using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.BlockedWords.CreateBlockedWord;

/// <summary>Adds a word/phrase to the profanity filter list.</summary>
/// <remarks>Implements: BR-REP-004, BR-CMT-003, BR-ADM-010.</remarks>
public sealed record CreateBlockedWordCommand(
    string Word,
    string? Note = null) : IRequest<Result<CreateBlockedWordResponse>>;

public sealed record CreateBlockedWordResponse(Guid Id, string Word, bool IsActive);
