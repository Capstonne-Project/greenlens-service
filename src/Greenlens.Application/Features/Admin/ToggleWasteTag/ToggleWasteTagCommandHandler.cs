using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ToggleWasteTag;

public sealed class ToggleWasteTagCommandHandler(
    IWasteTagRepository wasteTags,
    IUnitOfWork uow) : IRequestHandler<ToggleWasteTagCommand, Result>
{
    public async Task<Result> Handle(ToggleWasteTagCommand request, CancellationToken ct)
    {
        var tag = await wasteTags.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (tag is null)
            return Errors.Reports.WasteTagNotFound;

        if (request.IsActive)
            tag.Activate();
        else
            tag.Deactivate();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
