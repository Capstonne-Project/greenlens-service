using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Auth.ChangePassword;

/// <summary>Change password for authenticated user.</summary>
/// <remarks>
/// When a CompanyManager changes password for the first time (MustChangePassword flag),
/// the associated company is automatically activated (PendingActivation → Active).
/// </remarks>
public sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    ICompanyStaffRepository companyStaff,
    IEnvironmentalServiceCompanyRepository companies,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    ILogger<ChangePasswordCommandHandler> logger)
    : IRequestHandler<ChangePasswordCommand, Result<ChangePasswordResponse>>
{
    public async Task<Result<ChangePasswordResponse>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        // Find authenticated user
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return Errors.Auth.UserNotFound;

        // Verify current password before allowing change
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            logger.LogWarning("Incorrect current password for user {UserId}", currentUser.UserId);
            return Errors.Auth.IncorrectCurrentPassword;
        }

        var wasFirstLogin = user.MustChangePassword;

        // Apply new password hash (also clears MustChangePassword flag if set)
        user.ChangePassword(passwordHasher.Hash(request.NewPassword));

        // ── Auto-activate company when CM changes first password ──
        if (wasFirstLogin && user.Role == UserRole.CompanyManager)
        {
            var staff = await companyStaff.QueryAsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (staff is not null)
            {
                var company = await companies.GetByIdAsync(staff.CompanyId, cancellationToken)
                    .ConfigureAwait(false);

                if (company is not null && company.Status == Domain.Entities.CompanyStatus.PendingActivation)
                {
                    company.Activate();
                    logger.LogInformation(
                        "Company {CompanyId} auto-activated after CM {UserId} changed first password",
                        company.Id, user.Id);
                }
            }
        }

        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Password changed successfully for user {UserId}", currentUser.UserId);

        return new ChangePasswordResponse("Đổi mật khẩu thành công.");
    }
}
