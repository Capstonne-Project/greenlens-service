using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.RequestAccountDeletion;

/// <summary>
/// User requests deletion of their own account.
/// Soft-deletes the user; after 90 days the AccountHardDeleteJob permanently removes data.
/// </summary>
/// <remarks>Implements: BR-AUTH-021 (soft delete 90 days, anonymize reports).</remarks>
public sealed class RequestAccountDeletionCommandHandler(
    IUserRepository users,
    IReportRepository reports,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ISystemSettingsProvider systemSettings,
    ILogger<RequestAccountDeletionCommandHandler> logger)
    : IRequestHandler<RequestAccountDeletionCommand, Result<RequestAccountDeletionResponse>>
{
    public async Task<Result<RequestAccountDeletionResponse>> Handle(
        RequestAccountDeletionCommand request,
        CancellationToken cancellationToken)
    {
        var retentionDays = ModuleSystemSettings.AccountSoftDeleteRetentionDays(systemSettings);

        logger.LogInformation("Getting account deletion");

        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            logger.LogWarning("User not found for user {UserId}", currentUser.UserId);
            return Errors.Auth.UserNotFound;
        }

        if (user.IsDeleted)
        {
            logger.LogWarning("User {UserId} already deleted", currentUser.UserId);
            return Errors.Users.UserAlreadyDeleted;
        }

        user.SoftDelete(currentUser.Email);

        var anonymizedCount = await reports
            .AnonymizeReporterAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Anonymized count: {AnonymizedCount}", anonymizedCount);

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var deletionDate = DateTime.UtcNow.AddDays(retentionDays);

        logger.LogInformation(
            "User {UserId} requested account deletion. {ReportCount} reports anonymized. Will be hard-deleted after {Date}",
            user.Id, anonymizedCount, deletionDate);

        return new RequestAccountDeletionResponse(
            $"Tài khoản sẽ được xóa vĩnh viễn sau {retentionDays} ngày. Bạn có thể khôi phục trước thời hạn.",
            deletionDate);
    }
}
