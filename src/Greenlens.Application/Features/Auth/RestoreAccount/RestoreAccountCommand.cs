using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Auth.RestoreAccount;

/// <summary>
/// BR-AUTH-021: Restore a soft-deleted account before the 90-day deadline.
/// User must provide credentials since they cannot login while soft-deleted.
/// </summary>
public sealed record RestoreAccountCommand(string Email, string Password) : IRequest<Result<RestoreAccountResponse>>;
