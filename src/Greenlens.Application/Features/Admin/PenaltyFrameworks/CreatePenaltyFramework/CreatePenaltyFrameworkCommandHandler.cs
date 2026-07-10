using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.CreatePenaltyFramework;

/// <summary>
/// Creates a new PenaltyFramework entry.
/// </summary>
/// <remarks>Implements: BR-ADM-008.</remarks>
public sealed class CreatePenaltyFrameworkCommandHandler(
    IPollutionCategoryRepository pollutionCategories,
    IPenaltyFrameworkRepository penaltyFrameworks,
    IUnitOfWork uow)
    : IRequestHandler<CreatePenaltyFrameworkCommand, Result<CreatePenaltyFrameworkResponse>>
{
    public async Task<Result<CreatePenaltyFrameworkResponse>> Handle(
        CreatePenaltyFrameworkCommand request,
        CancellationToken ct)
    {
        // Verify category exists
        var categoryExists = await pollutionCategories
            .ExistsAsync(c => c.Id == request.CategoryId, ct)
            .ConfigureAwait(false);

        if (!categoryExists)
            return Result<CreatePenaltyFrameworkResponse>.Failure(
                new Error("PenaltyFramework.CategoryNotFound", "Pollution category not found.", ErrorType.NotFound));

        // Check for duplicate active entry: same category + level + active
        var duplicate = await penaltyFrameworks
            .ExistsAsync(p => p.CategoryId == request.CategoryId
                              && p.ViolationLevel == request.ViolationLevel
                              && p.IsActive, ct)
            .ConfigureAwait(false);

        if (duplicate)
            return Result<CreatePenaltyFrameworkResponse>.Failure(
                new Error("PenaltyFramework.Duplicate",
                    $"An active penalty framework already exists for this category and level '{request.ViolationLevel}'.",
                    ErrorType.Conflict));

        var entity = PenaltyFramework.Create(
            request.CategoryId,
            request.ViolationLevel,
            request.MinAmount,
            request.MaxAmount,
            request.EffectiveFrom,
            request.EffectiveTo);

        penaltyFrameworks.Add(entity);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return new CreatePenaltyFrameworkResponse(
            entity.Id,
            entity.CategoryId,
            entity.ViolationLevel.ToString(),
            entity.MinAmount,
            entity.MaxAmount,
            entity.EffectiveFrom);
    }
}
