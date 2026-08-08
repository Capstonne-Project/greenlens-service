using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.CreateWasteTag;

/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed class CreateWasteTagCommandHandler(
    IWasteTagRepository wasteTags,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<CreateWasteTagCommandHandler> logger) : IRequestHandler<CreateWasteTagCommand, Result<CreateWasteTagResponse>>
{
    public async Task<Result<CreateWasteTagResponse>> Handle(
        CreateWasteTagCommand request, CancellationToken ct)
    {
        // Check code uniqueness (include soft-deleted — DB unique index is not filtered)
        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await wasteTags.CodeExistsAsync(code, ct: ct).ConfigureAwait(false);
        if (exists)
        {
            logger.LogWarning("Waste tag code already exists: {Code}", code);
            return Errors.Reports.WasteTagCodeExists;
        }

        var tag = WasteTag.Create(
            code,
            request.NameVi,
            request.NameEn,
            request.IconUrl,
            request.Description,
            request.DisplayOrder);

        wasteTags.Add(tag);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("WasteTag {Code} created with id {TagId}", tag.Code, tag.Id);

        await auditLogger.LogAsync(
            "CreateWasteTag",
            "WasteTag",
            tag.Id.ToString(),
            oldValues: null,
            newValues: JsonSerializer.Serialize(new
            {
                tag.Code,
                tag.NameVi,
                tag.NameEn,
                tag.DisplayOrder
            }),
            ct).ConfigureAwait(false);

        return new CreateWasteTagResponse(tag.Id, tag.Code);
    }
}
