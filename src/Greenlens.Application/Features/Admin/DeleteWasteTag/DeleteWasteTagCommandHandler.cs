using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.DeleteWasteTag;

/// <summary>Admin soft-deletes a waste tag.</summary>
/// <remarks>Implements: BR-ADM-003 (catalog management), BR-DAT-002 (retain referential integrity).</remarks>
public sealed class DeleteWasteTagCommandHandler(
    IWasteTagRepository wasteTags,
    IReportWasteTagRepository reportWasteTags,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeleteWasteTagCommandHandler> logger) : IRequestHandler<DeleteWasteTagCommand, Result>
{
    public async Task<Result> Handle(DeleteWasteTagCommand request, CancellationToken ct)
    {
        var wasteTag = await wasteTags.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
        if (wasteTag is null)
        {
            logger.LogWarning("Waste tag not found: {Id}", request.Id);
            return Errors.Reports.WasteTagNotFound;
        }

        if (wasteTag.IsDeleted)
        {
            logger.LogWarning("Waste tag already deleted: {Id}", request.Id);
            return Errors.Reports.WasteTagAlreadyDeleted;
        }

        var inUse = await reportWasteTags.ExistsByWasteTagIdAsync(request.Id, ct).ConfigureAwait(false);
        if (inUse)
        {
            logger.LogWarning("Waste tag is in use: {Id}", request.Id);
            return Errors.Reports.WasteTagInUse;
        }

        wasteTag.SoftDelete(currentUser.UserId.ToString());
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("WasteTag {WasteTagId} soft-deleted by {UserId}", request.Id, currentUser.UserId);

        return Result.Success();
    }
}
