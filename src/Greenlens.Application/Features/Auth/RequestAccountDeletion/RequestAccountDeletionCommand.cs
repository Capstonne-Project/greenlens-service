using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.RequestAccountDeletion;

/// <summary>BR-AUTH-021: User requests their own account deletion (soft delete 90 days).</summary>
public sealed record RequestAccountDeletionCommand : IRequest<Result<RequestAccountDeletionResponse>>;
