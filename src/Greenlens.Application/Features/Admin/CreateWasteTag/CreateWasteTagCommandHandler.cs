using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Admin.CreateWasteTag;

public sealed class CreateWasteTagCommandHandler(
    IWasteTagRepository wasteTags,
    IUnitOfWork uow) : IRequestHandler<CreateWasteTagCommand, Result<CreateWasteTagResponse>>
{
    public async Task<Result<CreateWasteTagResponse>> Handle(
        CreateWasteTagCommand request, CancellationToken ct)
    {
        // Check code uniqueness
        var exists = await wasteTags.ExistsAsync(
            t => t.Code == request.Code, ct).ConfigureAwait(false);

        if (exists)
            return Errors.Reports.WasteTagCodeExists;

        var tag = WasteTag.Create(
            request.Code,
            request.NameVi,
            request.NameEn,
            request.IconUrl,
            request.Description,
            request.DisplayOrder);

        wasteTags.Add(tag);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return new CreateWasteTagResponse(tag.Id, tag.Code);
    }
}
