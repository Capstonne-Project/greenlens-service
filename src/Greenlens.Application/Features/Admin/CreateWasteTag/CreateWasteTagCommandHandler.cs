using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.CreateWasteTag;

public sealed class CreateWasteTagCommandHandler(
    IWasteTagRepository wasteTags,
    IUnitOfWork uow,
    ILogger<CreateWasteTagCommandHandler> logger) : IRequestHandler<CreateWasteTagCommand, Result<CreateWasteTagResponse>>
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

        logger.LogInformation("WasteTag {Code} created with id {TagId}", tag.Code, tag.Id);

        return new CreateWasteTagResponse(tag.Id, tag.Code);
    }
}
