using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.CommunityCleanup.CancelCommunityCleanup;

/// <summary>Draft BR-CMU-012: LEO cancels the program. Any status except Completed → Cancelled.</summary>
public sealed record CancelCommunityCleanupCommand(Guid EventId, string Reason) : IRequest<Result>;
