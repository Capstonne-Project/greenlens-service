using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.IssuePenalty;

/// <summary>
/// BR-INS-012: Only Team Leader can issue penalty.
/// BR-INS-022: Auto-detect repeat offender (≥ 2 times in 12 months).
/// </summary>
public sealed class IssuePenaltyCommandHandler(
    IInspectionReportRepository inspections,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<IssuePenaltyCommandHandler> logger)
    : IRequestHandler<IssuePenaltyCommand, Result>
{
    public async Task<Result> Handle(IssuePenaltyCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        var authError = await InspectionTeamAuthorization.ValidateTeamLeaderAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
            return authError;

        // BR-INS-022: Check repeat offender
        var isRepeatOffender = false;
        if (!string.IsNullOrWhiteSpace(inspection.ViolatorIdentity))
        {
            var count = await inspections.CountByViolatorInPeriodAsync(
                inspection.ViolatorIdentity, 12, ct).ConfigureAwait(false);
            isRepeatOffender = count >= 1; // current one will be the 2nd+
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

        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Penalty issued on InspectionReport {Id}: {Amount} VND, level {Level}, repeat={Repeat}",
            inspection.Id, request.PenaltyAmount, request.ViolationLevel, isRepeatOffender);

        return Result.Success();
    }
}
