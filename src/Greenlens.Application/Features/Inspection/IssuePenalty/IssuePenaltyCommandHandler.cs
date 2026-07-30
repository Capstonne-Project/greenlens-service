using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.IssuePenalty;

/// <summary>
/// BR-INS-012: Only Team Leader can issue penalty.
/// BR-INS-022: Auto-detect repeat offender (≥ 2 times in 12 months) via ViolatingEntityId (FK).
/// Falls back to string-match ViolatorIdentity if ViolatingEntityId is not linked.
/// </summary>
/// <remarks>Implements: BR-INS-011, BR-INS-012, BR-INS-022, BR-ADM-010.</remarks>
public sealed class IssuePenaltyCommandHandler(
    IInspectionReportRepository inspections,
    IViolatingEntityRepository violatingEntities,
    IReportMediaRepository reportMedia,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<IssuePenaltyCommandHandler> logger)
    : IRequestHandler<IssuePenaltyCommand, Result>
{
    public async Task<Result> Handle(IssuePenaltyCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting issue penalty");

        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        var authError = await InspectionTeamAuthorization.ValidateTeamLeaderAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
        {
            logger.LogWarning("Team leader validation failed for inspection {InspectionId}", request.InspectionId);
            return authError;
        }

        // BR-INS-010: Enforce ≥ 2 evidence images before issuing penalty
        var evidenceCount = await reportMedia.QueryAsNoTracking()
            .CountAsync(m => m.ReportId == inspection.ReportId && m.Type == MediaType.Inspection, ct)
            .ConfigureAwait(false);
        if (evidenceCount < 2)
        {
            logger.LogWarning("Insufficient evidence images for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InsufficientEvidenceImages;
        }

        // BR-INS-022: Check repeat offender
        var isRepeatOffender = false;

        if (inspection.ViolatingEntityId is not null)
        {
            // Preferred path: query by FK for accuracy
            var count = await violatingEntities.CountInspectionsInPeriodAsync(
                inspection.ViolatingEntityId.Value, 12, ct).ConfigureAwait(false);
            if (count >= 1) // current one will be the 2nd+
            {
                logger.LogWarning("Violating entity {ViolatingEntityId} is a repeat offender", inspection.ViolatingEntityId.Value);
                isRepeatOffender = true;
            }
        }
        else if (!string.IsNullOrWhiteSpace(inspection.ViolatorIdentity))
        {
            // Fallback: string-match for backward compatibility
            var count = await inspections.CountByViolatorInPeriodAsync(
                inspection.ViolatorIdentity, 12, ct).ConfigureAwait(false);
            if (count >= 1)
            {
                logger.LogWarning("Violator identity {ViolatorIdentity} is a repeat offender", inspection.ViolatorIdentity);
                isRepeatOffender = true;
            }
        }

        var dueDate = DateTime.UtcNow.AddDays(request.PaymentDueDays);

        var result = inspection.IssuePenalty(
            currentUser.UserId,
            request.ViolationLevel,
            request.PenaltyAmount,
            request.DecisionNumber,
            dueDate,
            request.AdditionalMeasures,
            isRepeatOffender);

        if (result.IsFailure)
        {
            logger.LogWarning("Failed to issue penalty for inspection {InspectionId}", request.InspectionId);
            return result;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Penalty issued on InspectionReport {Id}: {Amount} VND, level {Level}, repeat={Repeat}, violatingEntityId={VeId}",
            inspection.Id, request.PenaltyAmount, request.ViolationLevel, isRepeatOffender,
            inspection.ViolatingEntityId);

        return Result.Success();
    }
}
