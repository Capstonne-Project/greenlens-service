using Greenlens.Application.Common.Idempotency;
using Greenlens.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Greenlens.Infrastructure.Idempotency;

internal sealed class IdempotencyContext(IHttpContextAccessor httpContextAccessor) : IIdempotencyContext
{
    public bool IsReplay =>
        httpContextAccessor.HttpContext?.Items[IdempotencyHttpItems.IsReplayKey] as bool? == true;
}
