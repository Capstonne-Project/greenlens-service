using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.CreatePenaltyFramework;

/// <summary>
/// Creates a new PenaltyFramework entry.
/// </summary>
/// <remarks>Implements: BR-ADM-008, BR-ADM-010.</remarks>
public sealed class CreatePenaltyFrameworkCommandHandler(
    IPollutionCategoryRepository pollutionCategories,
    IPenaltyFrameworkRepository penaltyFrameworks,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<CreatePenaltyFrameworkCommandHandler> logger)
    : IRequestHandler<CreatePenaltyFrameworkCommand, Result<CreatePenaltyFrameworkResponse>>
{
    public async Task<Result<CreatePenaltyFrameworkResponse>> Handle(
        CreatePenaltyFrameworkCommand request,
        CancellationToken ct)
    {
        logger.LogInformation("Creating penalty framework");

        // Verify category exists
        var categoryExists = await pollutionCategories
            .ExistsAsync(c => c.Id == request.CategoryId, ct)
            .ConfigureAwait(false);

        if (!categoryExists)
        {
            logger.LogWarning("Category not found: {CategoryId}", request.CategoryId);
            return Result<CreatePenaltyFrameworkResponse>.Failure(Errors.Admin.PenaltyFrameworkCategoryNotFound);
        }

        // Check for duplicate active entry: same category + level + active
        var duplicate = await penaltyFrameworks
            .ExistsAsync(p => p.CategoryId == request.CategoryId
                              && p.ViolationLevel == request.ViolationLevel
                              && p.IsActive, ct)
            .ConfigureAwait(false);

        if (duplicate)
        {
            logger.LogWarning("Duplicate penalty framework: {CategoryId} {ViolationLevel}", request.CategoryId, request.ViolationLevel);
            return Result<CreatePenaltyFrameworkResponse>.Failure(
                Errors.Admin.PenaltyFrameworkDuplicate(request.ViolationLevel.ToString()));
        }

        var entity = PenaltyFramework.Create(
            request.CategoryId,
            request.ViolationLevel,
            request.MinAmount,
            request.MaxAmount,
            request.EffectiveFrom,
            request.EffectiveTo);

        penaltyFrameworks.Add(entity);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Penalty framework created successfully: {Id}", entity.Id);

        await auditLogger.LogAsync(
            "CreatePenaltyFramework",
            "PenaltyFramework",
            entity.Id.ToString(),
            oldValues: null,
            newValues: JsonSerializer.Serialize(new
            {
                entity.CategoryId,
                ViolationLevel = entity.ViolationLevel.ToString(),
                entity.MinAmount,
                entity.MaxAmount,
                entity.EffectiveFrom,
                entity.EffectiveTo
            }),
            ct).ConfigureAwait(false);

        return new CreatePenaltyFrameworkResponse(
            entity.Id,
            entity.CategoryId,
            entity.ViolationLevel.ToString(),
            entity.MinAmount,
            entity.MaxAmount,
            entity.EffectiveFrom);
    }
}
