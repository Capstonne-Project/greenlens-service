using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.BlockedWords.DeleteBlockedWord;

/// <summary>Deactivates a blocked word (soft-remove from active filter list).</summary>
/// <remarks>Implements: BR-REP-004, BR-CMT-003, BR-ADM-010.</remarks>
public sealed record DeleteBlockedWordCommand(Guid Id) : IRequest<Result>;
