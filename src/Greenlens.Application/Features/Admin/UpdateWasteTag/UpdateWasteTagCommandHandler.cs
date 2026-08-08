using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.UpdateWasteTag;

public sealed class UpdateWasteTagCommandHandler(
    IWasteTagRepository wasteTags,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<UpdateWasteTagCommandHandler> logger) : IRequestHandler<UpdateWasteTagCommand, Result>
{
    public async Task<Result> Handle(UpdateWasteTagCommand request, CancellationToken ct)
    {
        var tag = await wasteTags.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (tag is null)
        {
            logger.LogWarning("Waste tag not found: {Id}", request.Id);
            return Errors.Reports.WasteTagNotFound;
        }

        if (tag.IsDeleted)
        {
            logger.LogWarning("Waste tag already deleted: {Id}", request.Id);
            return Errors.Reports.WasteTagAlreadyDeleted;
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            tag.NameVi,
            tag.NameEn,
            tag.IconUrl,
            tag.DisplayOrder,
            tag.IsActive
        });

        tag.Update(request.NameVi, request.NameEn, request.IconUrl,
            request.Description, request.DisplayOrder);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "UpdateWasteTag",
            "WasteTag",
            tag.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                tag.NameVi,
                tag.NameEn,
                tag.IconUrl,
                tag.DisplayOrder,
                tag.IsActive
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation("WasteTag {TagId} updated", request.Id);

        return Result.Success();
    }
}
