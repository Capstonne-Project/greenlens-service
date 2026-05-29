using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.UpdateWasteTag;

public sealed class UpdateWasteTagCommandHandler(
    IWasteTagRepository wasteTags,
    IUnitOfWork uow,
    ILogger<UpdateWasteTagCommandHandler> logger) : IRequestHandler<UpdateWasteTagCommand, Result>
{
    public async Task<Result> Handle(UpdateWasteTagCommand request, CancellationToken ct)
    {
        var tag = await wasteTags.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (tag is null)
            return Errors.Reports.WasteTagNotFound;

        tag.Update(request.NameVi, request.NameEn, request.IconUrl,
            request.Description, request.DisplayOrder);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("WasteTag {TagId} updated", request.Id);

        return Result.Success();
    }
}
