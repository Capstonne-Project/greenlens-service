using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.BlockedWords.UpdateBlockedWord;

/// <summary>Updates a blocked word entry (word text, note, active flag).</summary>
/// <remarks>Implements: BR-REP-004, BR-CMT-003, BR-ADM-010.</remarks>
public sealed record UpdateBlockedWordCommand(
    Guid Id,
    string Word,
    string? Note,
    bool IsActive) : IRequest<Result>;
