using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.DeleteWasteTag;

public sealed class DeleteWasteTagCommandHandler(
    IWasteTagRepository wasteTags,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeleteWasteTagCommandHandler> logger) : IRequestHandler<DeleteWasteTagCommand, Result>
{
    public async Task<Result> Handle(DeleteWasteTagCommand request, CancellationToken ct)
    {
        var wasteTag = await wasteTags.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (wasteTag is null)
            return Errors.Reports.WasteTagNotFound;

        wasteTag.SoftDelete(currentUser.UserId.ToString());
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("WasteTag {WasteTagId} soft-deleted by {UserId}", request.Id, currentUser.UserId);

        return Result.Success();
    }
}
